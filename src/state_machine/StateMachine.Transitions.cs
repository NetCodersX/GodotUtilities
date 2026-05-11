using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Utilities.FSM;

public partial class StateMachine<T>
{
    public const int TRANSITION_CACHE_INITIAL_CAPACITY = 32;

    private readonly List<Transition<T>> globalTransitions = new();

    private Transition<T>[] transitionCache = new Transition<T>[TRANSITION_CACHE_INITIAL_CAPACITY];

    private int transitionCounter;
    private int transitionCacheCount;

    #region Transitions Update

    private bool ShouldSkipTransition(Transition<T> transition) =>
        GetState(transition.To).IsOnCooldown(totalTime) ||
        transition.IsGuardBlocked() ||
        !transition.IsMinTimeExceeded(elapsed, currentState.MinDuration);

    private void OnTransitionSelected(Transition<T> transition)
    {
        GetState(transition.To).LastFiredTime = totalTime;
        transition.Callback?.Invoke();
        TransitionTo(transition.To);
    }

    private void UpdateTransitions()
    {
        var span = transitionCache.AsSpan(0, transitionCacheCount);

        foreach (ref readonly var transition in span)
        {
            if (ShouldSkipTransition(transition))
                continue;
        
            if (transition.Condition?.Invoke() ?? false)
            {
                OnTransitionSelected(transition);
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

    #region Add

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

    #endregion

    #region Remove

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

    #endregion

    #region Internal API

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
        elapsed = 0.0;
        currentState.Enter?.Invoke();

        StateChanged?.Invoke(previousId, currentState.Id);
        RebuildTransitionCache();
    }

    #endregion

    #region Utilities

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

    public void ForceTransition(T id) => TransitionTo(id, respectLocked: false);

    #endregion


}