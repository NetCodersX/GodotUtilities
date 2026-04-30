using System;
using Godot;

namespace Utilities.FSM;

public partial class StateMachine<T>
{
    #region Trigger Event

    public void TriggerEvent(StringName eventName) =>
        pendingEvents.Enqueue(eventName);

    #endregion
    
    #region On Event

    public void AddEventListener(StringName eventName, Action callback)
    {
        if (!eventListeners.ContainsKey(eventName))
            eventListeners[eventName] = new();

        if (!eventListeners[eventName].Contains(callback))
            eventListeners[eventName].Add(callback);
    }

    #endregion

    #region Remove Event
    
    public void RemoveEventListener(StringName eventName, Action callback)
    {
        if (!eventListeners.TryGetValue(eventName, out var list))
            return;

        list.Remove(callback);
        
        if (list.Count == 0)
            eventListeners.Remove(eventName);
    }

    #endregion

    #region Events Update

    private void ProcessEvents()
    {
        while (pendingEvents.Count > 0)
        {
            var eventName = pendingEvents.Dequeue();

            if (eventListeners.TryGetValue(eventName, out var list))
                for (int i = list.Count - 1; i >= 0; i--)
                    list[i]?.Invoke();
            
            var span = transitionCache.AsSpan(0, transitionCacheCount);

            foreach (ref readonly var transition in span)
            {                
                if (transition.EventName != eventName) continue;
                if (transition.IsGuardBlocked())       continue;
                if (!transition.IsMinTimeExceeded(elapsed, currentState.MinDuration)) continue;

                transition.Callback?.Invoke();
                TransitionTo(transition.To);
                return;
            }
        }
    }
    
    #endregion
}

