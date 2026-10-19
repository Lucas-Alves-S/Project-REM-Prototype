using System;
using System.Collections.Generic;
using Godot;

namespace ProjectREM.Combat.Rng;

/// <summary>
/// A generic Shuffle Bag that draws items without replacement in O(1) time.
/// When all items have been drawn, it automatically resets and continues.
/// </summary>
/// <typeparam name="T">The type of item/marble contained in the bag.</typeparam>
public class MarbleBag<T>
{
    private readonly List<T> _marbles = new();
    private int _remaining;
    private Func<int, int, int> _rangeFunc;
    private RandomNumberGenerator _godotRng;

    public int TotalCount => _marbles.Count;
    public int RemainingCount => _remaining;
    public bool IsEmpty => _remaining == 0;
    public bool AutoRefill { get; set; } = true;

    /// <summary>
    /// Optional Godot RandomNumberGenerator. When set, RNG calls are delegated to it.
    /// When null, high-performance thread-safe Random.Shared is used.
    /// </summary>
    public RandomNumberGenerator GodotRng
    {
        get => _godotRng;
        set
        {
            _godotRng = value;
            _rangeFunc =
                value != null
                    ? (min, max) => value.RandiRange(min, max)
                    : ((min, max) => Random.Shared.Next(min, max + 1));
        }
    }

    /// <summary>
    /// Event fired whenever the bag is depleted and refilled back to capacity.
    /// </summary>
    public event Action Refilled;

    public MarbleBag()
    {
        _rangeFunc = (min, max) => Random.Shared.Next(min, max + 1);
    }

    public MarbleBag(RandomNumberGenerator rng)
    {
        GodotRng = rng;
    }

    public MarbleBag(Random random)
    {
        if (random != null)
            _rangeFunc = (min, max) => random.Next(min, max + 1);
        else
            _rangeFunc = (min, max) => Random.Shared.Next(min, max + 1);
    }

    public MarbleBag(Func<int, int, int> customRangeFunc)
    {
        _rangeFunc = customRangeFunc ?? ((min, max) => Random.Shared.Next(min, max + 1));
    }

    /// <summary>
    /// Adds one or more copies of an item to the bag.
    /// </summary>
    public void Add(T item, int count = 1)
    {
        if (count <= 0)
            return;
        for (int i = 0; i < count; i++)
        {
            _marbles.Add(item);
        }
        _remaining = _marbles.Count;
    }

    /// <summary>
    /// Clears the bag and sets a brand new composition.
    /// </summary>
    public void SetComposition(IEnumerable<(T item, int count)> composition)
    {
        _marbles.Clear();
        foreach (var (item, count) in composition)
        {
            for (int i = 0; i < count; i++)
            {
                _marbles.Add(item);
            }
        }
        _remaining = _marbles.Count;
    }

    /// <summary>
    /// Removes all marbles from the bag.
    /// </summary>
    public void Clear()
    {
        _marbles.Clear();
        _remaining = 0;
    }

    /// <summary>
    /// Resets the remaining pool so all marbles in the bag are available to be drawn again.
    /// </summary>
    public void Reset()
    {
        _remaining = _marbles.Count;
    }

    /// <summary>
    /// Draws a random marble from the remaining pool in O(1) time without replacement.
    /// Automatically triggers a refill if the bag becomes empty and AutoRefill is enabled.
    /// </summary>
    public T Draw()
    {
        if (_remaining <= 0)
        {
            if (AutoRefill && _marbles.Count > 0)
            {
                Reset();
                Refilled?.Invoke();
            }
            else
            {
                throw new InvalidOperationException("Cannot draw from an empty MarbleBag.");
            }
        }

        int pickIndex = _rangeFunc(0, _remaining - 1);
        T selected = _marbles[pickIndex];

        // Swap picked marble with the last unpicked marble to keep unpicked items in [0 .. _remaining - 1]
        _marbles[pickIndex] = _marbles[_remaining - 1];
        _marbles[_remaining - 1] = selected;
        _remaining--;

        return selected;
    }

    /// <summary>
    /// Peeks at a random marble from the remaining pool without consuming or moving it.
    /// </summary>
    public T Peek()
    {
        if (_remaining <= 0)
        {
            if (AutoRefill && _marbles.Count > 0)
            {
                Reset();
                Refilled?.Invoke();
            }
            else
            {
                throw new InvalidOperationException("Cannot peek into an empty MarbleBag.");
            }
        }

        int pickIndex = _rangeFunc(0, _remaining - 1);
        return _marbles[pickIndex];
    }

    /// <summary>
    /// Returns a snapshot copy of all marbles currently still in the active pool.
    /// </summary>
    public IReadOnlyList<T> GetRemainingMarbles()
    {
        if (_remaining <= 0)
            return Array.Empty<T>();
        var result = new T[_remaining];
        for (int i = 0; i < _remaining; i++)
        {
            result[i] = _marbles[i];
        }
        return result;
    }

    internal List<T> InternalMarbles => _marbles;

    internal void SetRemainingCountDirectly(int count) => _remaining = count;
}
