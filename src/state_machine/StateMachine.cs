using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Utilities.FSM;

public partial class StateMachine<T> where T : Enum
{
    public event Action<T>    StateTimeout;
    public event Action<T, T> StateChanged;

    public const int TRANSITION_CACHE_INITIAL_CAPACITY = 32;

    public float TimeInState   => elapsed;
    public float RemainingTime => currentState.Timeout.HasValue ? Mathf.Max(0f, currentState.Timeout.Value - elapsed)       : -1f;
    public float NormalizedTime => currentState.Timeout.HasValue ? Mathf.Clamp(elapsed / currentState.Timeout.Value, 0f, 1f) : -1f;

    public T CurrentStateId => currentState != null ? currentState.Id : default;
    public T PreviousId     => previousId;

    private readonly Dictionary<T, State<T>> states                      = new(EqualityComparer<T>.Default);
    private readonly Dictionary<StringName, List<Action>> eventListeners = new();

    private readonly List<Transition<T>> globalTransitions = new();
    private readonly Queue<StringName> pendingEvents = new();

    private Transition<T>[] transitionCache = new Transition<T>[TRANSITION_CACHE_INITIAL_CAPACITY];

    private State<T> currentState;

    private T initialId;
    private T previousId;

    private float elapsed;

    private bool initialized;
    private bool cacheDirty;
    private bool locked;
    private bool paused;

    private int transitionCounter;
    private int transitionCacheCount;

    private bool ValidateId(T id)
    {
        if (!states.ContainsKey(id))
        {
            GD.PushError($"Invalid State Id: '{id}' ");
            return false;
        }
        return true;
    }

    private void TransitionTo(T id, bool bypassExit = false, bool respectLocked = true)
    {   
        if (!ValidateId(id))
            return;
        
        if (locked && respectLocked)
            return;
    
        if (!bypassExit && currentState != null)
            currentState.Exit?.Invoke();
    
        if (currentState != null)
            previousId = currentState.Id;

        currentState = states[id];
        elapsed = 0f;
        currentState.Enter?.Invoke();

        StateChanged?.Invoke(previousId, currentState.Id);

        RebuildTransitionCache();
    }

    #region Initialization

    public void Start()
    {
        if (!initialized)
        {
            GD.PushError("invalid initial id, call SetInitialState() first");
            return;
        }

        TransitionTo(initialId, bypassExit: true);
    }

    public void Stop()
    {
        currentState?.Exit?.Invoke();
        currentState = null;
        initialized  = false;
    }

    public void SetInitialState(T id)
    {
        if (!states.ContainsKey(id))
        {
            GD.PushError($"invalid state id: {id}");
            return;
        }

        initialId = id;
        initialized = true;
    }

    public void Reset()
    {
        TransitionTo(initialId, respectLocked: false);
    }

    #endregion

    #region States Update

    public void Update(double dt) => Update((float)dt);
    
    public void Update(float dt)
    {
        if (currentState == null || paused)
            return;

        if (cacheDirty)
            RebuildTransitionCache();
        
        currentState.Update?.Invoke(dt);
        elapsed += dt;

        if (locked)
            return;

        if (OnStateTimeout())
            return;

        var startingState = currentState;
        ProcessEvents();

        // wait for the next frame
        if (currentState != startingState) 
            return;

        UpdateTransitions();
    }

    #endregion

    #region Transitions Update

    private void UpdateTransitions()
    {
        var span = transitionCache.AsSpan(0, transitionCacheCount);

        foreach (ref readonly var transition in span)
        {
            if (transition.IsGuardBlocked()) continue;
            if (!transition.IsMinTimeExceeded(elapsed, currentState.MinDuration)) continue;
        
            if (transition.Condition?.Invoke() ?? false)
            {
                transition.Callback?.Invoke();
                TransitionTo(transition.To);
                return;
            }
        }
    }

    private bool OnStateTimeout()
    {
        if (currentState.Timeout == null || elapsed < currentState.Timeout)
            return false;
    
        var fromId    = currentState.Id;
        var timeoutId = currentState.TimeoutTargetState;

        if (!states.ContainsKey(timeoutId))
        {
            GD.PushError($"Invalid timeout state id: {timeoutId}");
            return false;
        }

        currentState.OnTimeoutCallback?.Invoke();
        TransitionTo(timeoutId);
        StateTimeout?.Invoke(fromId);

        return true;
    }

    private void RebuildTransitionCache()
    {
        cacheDirty = false;

        var local  = currentState.Transitions;
        var global = globalTransitions;

        transitionCacheCount = local.Count + global.Count;

        if (transitionCacheCount > transitionCache.Length)
            Array.Resize(ref transitionCache, transitionCacheCount * 2);
        
        CollectionsMarshal.AsSpan(local).CopyTo(transitionCache);
        CollectionsMarshal.AsSpan(global).CopyTo(transitionCache.AsSpan(local.Count));
    }

    #endregion
    
    #region S & T Implementation

    public State<T> AddState(T id)
    {
        if (states.TryGetValue(id, out State<T> value))
        {
            GD.PushWarning($"State with id: '{id}' exists already");
            return value;
        }

        var state  = new State<T>(id);
        states[id] = state;

        return state;
    }

    public bool RemoveState(T id)
    {
        if (!states.TryGetValue(id, out var state))
            return false;

        if (EqualityComparer<T>.Default.Equals(id, initialId))
        {
            GD.PushWarning("Invalid Remove Operation. id equals initial id, make sure to set initial id to another state first");
            return false;
        }

        int tRemoved = 0;

        foreach (var s in states.Values)
            for (int i = s.Transitions.Count - 1; i >= 0; i--)
                if (EqualityComparer<T>.Default.Equals(s.Transitions[i].To, id))
                {
                    s.Transitions.RemoveAt(i);
                    tRemoved++;
                }

        for (int i = globalTransitions.Count - 1; i >= 0; i--)
        {
            if (EqualityComparer<T>.Default.Equals(globalTransitions[i].To, id))
            {
                globalTransitions.RemoveAt(i);
                tRemoved++;
            }
        }

        if (state == currentState) Reset();
        else if (tRemoved > 0)     RebuildTransitionCache();

        states.Remove(id);
        return true;
    }

    public Transition<T> AddTransition(T from, T to)
    {
        if (!ValidateId(from) || !ValidateId(to))
            return null;

        var transition = states[from].AddTransition(to, transitionCounter++);
        cacheDirty     = true;

        return transition;
    }

    public Transition<T> AddGlobalTransition(T to)
    {
        if (!ValidateId(to)) return null;

        var transition = new Transition<T>(default, to) { InsertionIndex = transitionCounter++ };
        
        SortedInsert(globalTransitions, transition);
        cacheDirty = true;
        return transition;
    }

    public bool RemoveTransition(Transition<T> transition)
    {
        if (!states.TryGetValue(transition.From, out var state))
            return false;
        
        var success = state.RemoveTransition(transition);
        if (success) cacheDirty = true;
        return success;
    }

    public bool RemoveGlobalTransition(Transition<T> transition)
    {
        if (!globalTransitions.Contains(transition))
            return false;
        
        globalTransitions.Remove(transition);
        cacheDirty = true;
        return true;
    }

    internal static void SortedInsert(List<Transition<T>> list, Transition<T> transition)
    {
        int lo = 0, hi = list.Count;

        while (lo < hi)
        {
            int mid = (lo + hi) / 2;

            if (Transition<T>.Compare(list[mid], transition) <= 0)
                lo = mid + 1;
            else
                hi = mid;
        }
        list.Insert(lo, transition);
    }

    #endregion

    #region Manual Control

    public bool CanTransition(T id)
    {
        if (currentState == null) return false;

        return ValidateId(id) && elapsed > currentState.MinDuration && !locked;
    }

    public bool TryTransition(T id)
    {
        if (!CanTransition(id)) return false;
        TransitionTo(id);
        return true;
    }

    public void ForceTransition(T id)
    {
        TransitionTo(id, respectLocked: false);
    }

    #endregion

    #region Queries

    public void Pause()  => paused = true;
    public void Resume() => paused = false;

    public void Lock()   => locked = true;
    public void Unlock() => locked = false;

    public bool IsInState(T id) => currentState != null ? EqualityComparer<T>.Default.Equals(id, currentState.Id) : false;
    public bool HasState(T id)  => states.ContainsKey(id);

    public State<T> GetState(T id) => TryGetState(id, out var state) ? state : default;
    public bool TryGetState(T id, out State<T> state) => states.TryGetValue(id, out state);

    #endregion

    #region Debug

    public string GetDebugInfo()
    {
        if (currentState == null)
            return "State machine not started.";

        return $"Current State: {currentState.Id}\nTransition: {previousId} -> {currentState.Id}\nTime Elapsed: {elapsed}";
    }

    #endregion
}
