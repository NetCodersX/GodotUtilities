using Godot;
using System;

namespace Utilities.FSM;

public class Transition<T> where T : Enum
{
    public T From { get; private set; }
    public T To { get; private set; }

    public Action Callback { get; private set; }

    public Func<bool> Condition { get; private set; }
    public Func<bool> Guard { get; private set; }

    public StringName EventName { get; private set; }

    public double? MinTimeOverride { get; private set; }

    public bool IgnoreMinDuration { get; private set; }

    public int Priority { get; private set; }
    public int InsertionIndex { get; set; }

    public Transition(T from, T to)
    {
        To = to;
        From = from;
    }

    public Transition<T> When(Func<bool> condition)
    {
        Condition = condition;
        return this;
    }

    public Transition<T> IfOnly(Func<bool> guard)
    {
        Guard = guard;
        return this;
    }

    public Transition<T> OnEvent(StringName eventName)
    {
        EventName = eventName;
        return this;
    }

    public Transition<T> Do(Action callback)
    {
        Callback = callback;
        return this;
    }

    /// <summary>
    /// Force Instant method ignores previous state min time
    /// </summary>
    /// <returns></returns>
    public Transition<T> ForceInstant()
    {
        IgnoreMinDuration = true;
        return this;
    }

    /// <summary>
    /// How long you must stay in (From) state before going to (To) state
    /// </summary>
    /// <returns></returns>
    public Transition<T> OverrideMinDuration(double duration)
    {
        if (duration <= 0f)
            GD.PushWarning($"Invalid Minimum Time duration. Value: -> {duration} <- should be greater than zero");

        MinTimeOverride = Mathf.Max(0f, duration);
        return this;
    }

    public Transition<T> SetPriority(int priority)
    {
        if (priority < 0)
            GD.PushWarning($"Invalid priority: {priority}, value should be zero or greater");
        Priority = priority;
        return this;
    }

    internal static int Compare(Transition<T> a, Transition<T> b)
    {
        int priorityCompare = b.Priority.CompareTo(a.Priority);
        return priorityCompare != 0 ? priorityCompare : a.InsertionIndex.CompareTo(b.InsertionIndex);
    }

    internal bool IsMinTimeExceeded(double time, double fallbackMinTime)
    {
        double required = MinTimeOverride ?? fallbackMinTime;
        return time > required || IgnoreMinDuration;
    }

    internal bool IsGuardBlocked()
    {
        return Guard != null && !Guard();
    }
}
