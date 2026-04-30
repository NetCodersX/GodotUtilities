using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;

namespace Utilities.Inputs;

public class InputBuffer
{
    public event Action<string> Consumed;
    public event Action<string> Expired;

    private readonly List<BufferedAction> actions = new();

    public void BufferAction(string name, float duration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, 0f);

        var span = CollectionsMarshal.AsSpan(actions);

        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Name == name)
            {
                span[i] = span[i].WithDuration(duration);
                return;
            }
        }

        actions.Add(new BufferedAction { Name = name, RemainingTime = duration });
    }

    public bool TryConsume(string name)
    {
        var span = CollectionsMarshal.AsSpan(actions);

        for (int i = 0; i < span.Length; i++)
        {
            if (span[i].Name == name && span[i].IsValid())
            {
                Consumed?.Invoke(name);
                RemoveAtSwap(i);
                return true;
            }
        }

        return false;
    }

    public void Tick(float dt)
    {
        // span for fastest iteration
        var span = CollectionsMarshal.AsSpan(actions);

        for (int i = span.Length - 1; i >= 0; i--)
        {
            span[i] = span[i].Tick(dt);

            if (!span[i].IsValid())
            {
                Expired?.Invoke(span[i].Name);
                RemoveAtSwap(i);

                // refresh
                span = CollectionsMarshal.AsSpan(actions);
            }
        }
    }

    private void RemoveAtSwap(int index)
    {
        int last = actions.Count - 1;

        if (index != last)
            actions[index] = actions[last];

        actions.RemoveAt(last);
    }

    public void ConsumeAll()
    {
        for (int i = actions.Count - 1; i >= 0; i--)
        {
            Consumed?.Invoke(actions[i].Name);
            RemoveAtSwap(i);
        }
    }

    public IEnumerable<string> GetActionNames()
    {
        foreach (var a in actions) yield return a.Name;
    }       
}

public readonly struct BufferedAction
{
    public string Name { get; init; }
    public float RemainingTime { get; init; }

    public BufferedAction Tick(float dt) => this with { RemainingTime = RemainingTime - dt };
    public BufferedAction WithDuration(float duration) => this with { RemainingTime = duration };

    public bool IsValid() => RemainingTime > 0f;
}

