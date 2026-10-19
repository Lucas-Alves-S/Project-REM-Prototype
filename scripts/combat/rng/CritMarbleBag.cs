using System;
using Godot;

namespace ProjectREM.Combat.Rng;

/// <summary>
/// A specialized Shuffle Bag designed for Critical Hit calculations.
/// Ensures consistent crit distributions over discrete cycles,
/// eliminating unlucky streaks and streak clustering.
/// </summary>
public class CritMarbleBag
{
    public const int DefaultCapacity = 20;

    /// <summary>
    /// Event triggered whenever a critical hit marble is drawn.
    /// </summary>
    public event Action CritDrawn;

    /// <summary>
    /// Event triggered whenever a normal hit marble is drawn.
    /// </summary>
    public event Action NormalDrawn;

    /// <summary>
    /// Event triggered on every draw, passing true if critical, false if normal.
    /// </summary>
    public event Action<bool> MarbleDrawn;

    /// <summary>
    /// Event triggered when the bag is depleted and refilled back to full capacity.
    /// </summary>
    public event Action BagRefilled;

    private readonly MarbleBag<bool> _bag;
    private float _critChance = 0.10f;
    private int _capacity = DefaultCapacity;
    private StatUpdatePolicy _updatePolicy = StatUpdatePolicy.InstantRebuild;
    private float? _pendingCritChance;

    private int _totalCrits;
    private int _totalNormals;
    private int _remainingCrits;
    private int _remainingNormals;

    /// <summary>
    /// The base critical hit chance represented as a normalized value [0.0, 1.0].
    /// Changing this property will apply the current UpdatePolicy.
    /// </summary>
    public float CritChance
    {
        get => _critChance;
        set => SetCritChance(value, _updatePolicy);
    }

    /// <summary>
    /// Total number of marbles in a full bag cycle (defaults to 20 for 5% increments).
    /// </summary>
    public int Capacity
    {
        get => _capacity;
        set
        {
            int clamped = Mathf.Max(1, value);
            if (_capacity == clamped)
                return;
            _capacity = clamped;
            RebuildBag();
        }
    }

    /// <summary>
    /// Determines how the bag responds when CritChance changes mid-cycle.
    /// </summary>
    public StatUpdatePolicy UpdatePolicy
    {
        get => _updatePolicy;
        set => _updatePolicy = value;
    }

    /// <summary>
    /// Number of critical hit marbles remaining in the current cycle.
    /// </summary>
    public int RemainingCrits => _remainingCrits;

    /// <summary>
    /// Number of normal hit marbles remaining in the current cycle.
    /// </summary>
    public int RemainingNormals => _remainingNormals;

    /// <summary>
    /// Total number of unpicked marbles remaining in the current cycle.
    /// </summary>
    public int RemainingTotal => _bag.RemainingCount;

    /// <summary>
    /// Real-time odds of drawing a critical hit on the next draw from the remaining marbles.
    /// </summary>
    public float CurrentOdds =>
        RemainingTotal > 0 ? (float)_remainingCrits / RemainingTotal : _critChance;

    /// <summary>
    /// Initializes a CritMarbleBag with a default 10% crit chance and 20 marbles.
    /// </summary>
    public CritMarbleBag()
        : this(0.10f, DefaultCapacity) { }

    /// <summary>
    /// Creates a new CritMarbleBag with specified crit chance, capacity, and optional Godot RNG.
    /// </summary>
    /// <param name="critChance">Base crit chance [0.0, 1.0].</param>
    /// <param name="capacity">Total marbles per cycle (default 20).</param>
    /// <param name="godotRng">Optional custom Godot RandomNumberGenerator.</param>
    public CritMarbleBag(
        float critChance,
        int capacity = DefaultCapacity,
        RandomNumberGenerator godotRng = null
    )
    {
        _capacity = Mathf.Max(1, capacity);
        _critChance = Mathf.Clamp(critChance, 0.0f, 1.0f);
        _bag = godotRng != null ? new MarbleBag<bool>(godotRng) : new MarbleBag<bool>();
        _bag.Refilled += OnBagInternalRefilled;
        RebuildBag();
    }

    /// <summary>
    /// Creates a new CritMarbleBag with specified crit chance, capacity, and System.Random.
    /// </summary>
    public CritMarbleBag(float critChance, int capacity, Random random)
    {
        _capacity = Mathf.Max(1, capacity);
        _critChance = Mathf.Clamp(critChance, 0.0f, 1.0f);
        _bag = new MarbleBag<bool>(random);
        _bag.Refilled += OnBagInternalRefilled;
        RebuildBag();
    }

    /// <summary>
    /// Draws the next hit result. Returns true for a critical hit, false for a normal hit.
    /// O(1) time complexity with zero garbage collection allocations.
    /// </summary>
    public bool Draw()
    {
        bool isCrit = _bag.Draw();

        if (isCrit)
        {
            _remainingCrits--;
            CritDrawn?.Invoke();
        }
        else
        {
            _remainingNormals--;
            NormalDrawn?.Invoke();
        }

        MarbleDrawn?.Invoke(isCrit);
        return isCrit;
    }

    /// <summary>
    /// Peeks at the next random marble without consuming it.
    /// </summary>
    public bool Peek()
    {
        return _bag.Peek();
    }

    /// <summary>
    /// Resets the bag to full capacity and reshuffles immediately.
    /// Useful for resetting state between encounters to prevent card counting.
    /// </summary>
    public void Reset()
    {
        if (_pendingCritChance.HasValue)
        {
            _critChance = _pendingCritChance.Value;
            _pendingCritChance = null;
            RebuildBag();
            return;
        }

        _bag.Reset();
        _remainingCrits = _totalCrits;
        _remainingNormals = _totalNormals;
        BagRefilled?.Invoke();
    }

    /// <summary>
    /// Updates the crit chance using the specified policy or the bag's default policy.
    /// </summary>
    public void SetCritChance(float newChance, StatUpdatePolicy? policyOverride = null)
    {
        float clampedChance = Mathf.Clamp(newChance, 0.0f, 1.0f);
        StatUpdatePolicy policy = policyOverride ?? _updatePolicy;

        switch (policy)
        {
            case StatUpdatePolicy.InstantRebuild:
                _critChance = clampedChance;
                _pendingCritChance = null;
                RebuildBag();
                break;

            case StatUpdatePolicy.ProportionalAdjust:
                ApplyProportionalAdjustment(clampedChance);
                break;

            case StatUpdatePolicy.NextCycleDeferred:
                _pendingCritChance = clampedChance;
                break;
        }
    }

    private void RebuildBag()
    {
        _totalCrits = Mathf.RoundToInt(_capacity * _critChance);
        _totalNormals = _capacity - _totalCrits;
        _remainingCrits = _totalCrits;
        _remainingNormals = _totalNormals;

        _bag.SetComposition(
            new (bool item, int count)[] { (true, _totalCrits), (false, _totalNormals) }
        );

        BagRefilled?.Invoke();
    }

    private void ApplyProportionalAdjustment(float newChance)
    {
        _critChance = newChance;
        _pendingCritChance = null;

        int newTotalCrits = Mathf.RoundToInt(_capacity * newChance);
        int newTotalNormals = _capacity - newTotalCrits;
        _totalCrits = newTotalCrits;
        _totalNormals = newTotalNormals;

        int remainingTotal = _bag.RemainingCount;
        if (remainingTotal <= 0)
        {
            RebuildBag();
            return;
        }

        // Adjust remaining marbles in the pool [0 .. remainingTotal - 1]
        int targetRemainingCrits = Mathf.Clamp(
            Mathf.RoundToInt(remainingTotal * newChance),
            0,
            remainingTotal
        );
        int delta = targetRemainingCrits - _remainingCrits;

        var list = _bag.InternalMarbles;

        if (delta > 0)
        {
            // Need more crits: convert remaining normals into crits
            int converted = 0;
            for (int i = 0; i < remainingTotal && converted < delta; i++)
            {
                if (!list[i])
                {
                    list[i] = true;
                    converted++;
                }
            }
            _remainingCrits += converted;
            _remainingNormals -= converted;
        }
        else if (delta < 0)
        {
            // Need fewer crits: convert remaining crits into normals
            int targetRemove = -delta;
            int converted = 0;
            for (int i = 0; i < remainingTotal && converted < targetRemove; i++)
            {
                if (list[i])
                {
                    list[i] = false;
                    converted++;
                }
            }
            _remainingCrits -= converted;
            _remainingNormals += converted;
        }

        // Balance the already-drawn section [remainingTotal .. Capacity - 1] so the next full refill has exact counts
        int drawnSectionStart = remainingTotal;
        int neededDrawnCrits = _totalCrits - _remainingCrits;
        int currentDrawnCrits = 0;

        for (int i = drawnSectionStart; i < _capacity; i++)
        {
            if (list[i])
                currentDrawnCrits++;
        }

        int drawnDelta = neededDrawnCrits - currentDrawnCrits;
        if (drawnDelta > 0)
        {
            int converted = 0;
            for (int i = drawnSectionStart; i < _capacity && converted < drawnDelta; i++)
            {
                if (!list[i])
                {
                    list[i] = true;
                    converted++;
                }
            }
        }
        else if (drawnDelta < 0)
        {
            int targetRemove = -drawnDelta;
            int converted = 0;
            for (int i = drawnSectionStart; i < _capacity && converted < targetRemove; i++)
            {
                if (list[i])
                {
                    list[i] = false;
                    converted++;
                }
            }
        }
    }

    private void OnBagInternalRefilled()
    {
        if (_pendingCritChance.HasValue)
        {
            _critChance = _pendingCritChance.Value;
            _pendingCritChance = null;
            RebuildBag();
            return;
        }

        _remainingCrits = _totalCrits;
        _remainingNormals = _totalNormals;
        BagRefilled?.Invoke();
    }
}
