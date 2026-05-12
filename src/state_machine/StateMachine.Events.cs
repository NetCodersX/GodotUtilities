using Godot;
using System;
using System.Collections.Generic;

namespace Utilities.FSM;

public partial class StateMachine<T>
{
    private readonly Queue<StringName> pendingEvents = new();

    #region Trigger Event

    public void TriggerEvent(StringName eventName) =>
        pendingEvents.Enqueue(eventName);

    #endregion

    #region Events Update

    private void ProcessEvents()
    {
        while (pendingEvents.Count > 0)
        {
            var eventName = pendingEvents.Dequeue();
            var span = transitionCache.AsSpan(0, transitionCacheCount);

            foreach (ref readonly var transition in span)
            {                
                if (transition.EventName != eventName) continue;
                if (ShouldSkipTransition(transition)) continue;

                OnTransitionSelected(transition);
                return;
            }
        }
    }
    
    #endregion
}

