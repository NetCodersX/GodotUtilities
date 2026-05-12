using System.Collections.Generic;
using System;
using Godot;

namespace Utilities.FSM;

public class State<T> where T : Enum
{
    public T Id { get; private set; }
    public T TimeoutTargetState { get; private set; }

    public Action Exit { get; private set; }
    public Action Enter { get; private set; }
    public Action<double> Update { get; private set; }
    public Action OnTimeoutCallback { get; private set; }

    public double MinDuration { get; private set; }
    public double? Timeout { get; private set; }
    public double Cooldown { get; private set; }
    public double LastFiredTime { get; set; }

    public List<Transition<T>> Transitions { get; private set; }

    public State(T id)
    {
        Id = id;
        Transitions = new();
        LastFiredTime = double.NegativeInfinity;
    }

    public State<T> OnUpdate(Action<double> callback)
    {
        Update = callback;
        return this;
    }

    public State<T> OnEnter(Action callback)
    {
        Enter = callback;
        return this;
    }

    public State<T> OnExit(Action callback)
    {
        Exit = callback;
        return this;
    }

    public State<T> SetCooldown(double duration)
    {
        if (duration <= 0)
            GD.PushWarning($"Invalid Cooldown duration. Value: -> {duration} <- should be greater than zero");
        Cooldown = duration;
        return this;
    }

    public bool IsOnCooldown(double totalTime)
    {
        return totalTime < (LastFiredTime + Cooldown);
    }

    /// <summary>
    /// Separated timeout call. Can be usedfull than using Exit() method sometimes
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    public State<T> OnTimeout(Action callback)
    {
        OnTimeoutCallback = callback;
        return this;
    }

    /// <summary>
    /// Call it to make state exit automatically after a specific duration
    /// </summary>
    /// <param name="duration"> how long we will stay in this state </param>
    /// <param name="to"> where are we going after timeout ?</param>
    /// <returns></returns>
    public State<T> TimeoutAfter(double duration, T to)
    {
        Timeout = Mathf.Max(0f, duration);
        TimeoutTargetState = to;
        return this;
    }

    /// <summary>
    /// How long you must stay in that state before leaving again
    /// </summary>
    /// <param name="duration"></param>
    /// <returns></returns>
    public State<T> SetMinDuration(double duration)
    {
        if (duration <= 0f)
            GD.PushWarning($"Invalid Minimum Time duration. Value: -> {duration} <- should be greater than zero");

        MinDuration = Mathf.Max(0f, duration);
        return this;
    }

    /// <summary>
    /// internal method for transition addition, call stateMachine.AddTransition() instead
    /// </summary>
    /// <param name="to"></param>
    /// <returns></returns>
    internal Transition<T> AddTransition(T to, int insertionCounter)
    {
        var transition = new Transition<T>(Id, to) { InsertionIndex = insertionCounter };
        StateMachine<T>.SortedInsert(Transitions, transition);
        return transition;
    }   

    internal bool RemoveTransition(Transition<T> transition)
    {
        int index = Transitions.IndexOf(transition);
        if (index >= 0)
        {
            Transitions.RemoveAt(index);
            return true;
        }
        return false;
    }
}

