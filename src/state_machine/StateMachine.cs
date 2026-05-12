using Godot;
using System;
using System.Collections.Generic;

namespace Utilities.FSM;

public partial class StateMachine<T> where T : Enum
{
    public event Action<T> StateTimeout;
    public event Action<T, T> StateChanged;

    public double TimeInState => elapsed;

    public T CurrentStateId => currentState != null ? currentState.Id : default;
    public T PreviousId => previousId;

    private readonly Dictionary<T, State<T>> states = new(EqualityComparer<T>.Default);

    private State<T> currentState;

    private T initialId;
    private T previousId;

    private double elapsed;
    private double totalTime;

    private bool initialized;
    private bool cacheDirty;
    private bool locked;
    private bool paused;

    private bool ValidateId(T id)
    {
        if (!states.ContainsKey(id))
        {
            GD.PushError($"Invalid State Id: '{id}' ");
            return false;
        }
        return true;
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

    #endregion

    #region Update

    public void Tick(double dt)
    {
        if (currentState == null || paused)
            return;

        totalTime += dt;

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

    #region Sort

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

    #region Utilities

    public void Disable()
    {
        currentState?.Exit?.Invoke();
        currentState = null;
        initialized  = false;
        pendingEvents.Clear();
        transitionCacheCount = 0;
        elapsed = 0;
        totalTime = 0;
        locked = false;
        paused = false;
    }

    public void Reset() => TransitionTo(initialId, respectLocked: false);

    public void Pause()  => paused = true;
    public void Resume() => paused = false;

    public void Lock()   => locked = true;
    public void Unlock() => locked = false;

    public bool IsInState(T id) => currentState != null && EqualityComparer<T>.Default.Equals(id, currentState.Id);
    public bool HasState(T id)  => states.ContainsKey(id);

    public State<T> GetState(T id) => TryGetState(id, out var state) ? state : default;
    public bool TryGetState(T id, out State<T> state) => states.TryGetValue(id, out state);

    public double RemainingTime()
    {
        if (currentState == null || !currentState.Timeout.HasValue) return -1.0;
        return Mathf.Max(0.0, currentState.Timeout.Value - elapsed);
    }

    public double NormalizedTime()
    {
        if (currentState == null || !currentState.Timeout.HasValue) return -1.0;
        return Mathf.Clamp(elapsed / currentState.Timeout.Value, 0.0, 1.0);
    }

    #endregion
}
