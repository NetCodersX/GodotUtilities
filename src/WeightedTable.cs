using System.Collections.Generic;
using System;
using Godot;

namespace Utilities;

public class WeightedTable<T>(RandomNumberGenerator rng = null)
{
    private record Entry(T Item, float Weight);

    private readonly RandomNumberGenerator rng = rng ?? new();
    private readonly List<Entry> entries = new();

    private float total = 0f;

    public int Count => entries.Count;

    public WeightedTable<T> Add(T item, float weight)
    {
        if (weight <= 0f) 
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight cannot be negative.");

        entries.Add(new(item, weight));
        total += weight;
        return this;
    }

    public T Pick()
    {
        if (entries.Count == 0)
            throw new InvalidOperationException($"Can't pick an item of type '{typeof(T).Name}' because the weighted table is empty.");

        float random = rng.RandfRange(0f, total);
        float acc = 0f;

        foreach (var entry in entries)
        {
            acc += entry.Weight;

            if (random < acc)
                return entry.Item;
        }

        return entries[^1].Item;
    }

    public void Clear()
    {
        entries.Clear();
        total = 0f;
    }

    public IEnumerable<T> PickMany(int count)
    {
        for (int i = 0; i < count; i++)
            yield return Pick();
    }

    public float GetProbability(T item)
    {
        foreach (var entry in entries)
            if (EqualityComparer<T>.Default.Equals(entry.Item, item))
                return entry.Weight / total;
        return 0f;
    }
}

