namespace ProjectREM.Combat.Rng;

/// <summary>
/// Defines how a MarbleBag reacts when its probability stats change mid-cycle
/// (e.g., when buffs, debuffs, or equipment modifications are applied).
/// </summary>
public enum StatUpdatePolicy
{
	/// <summary>
	/// Immediately rebuilds the bag with the new ratio and resets the draw state.
	/// Best for immediate, transparent feedback when gear or buffs change.
	/// </summary>
	InstantRebuild,

	/// <summary>
	/// Adjusts the remaining pool of marbles proportionally to match the new chance
	/// without resetting the current cycle's progress.
	/// </summary>
	ProportionalAdjust,

	/// <summary>
	/// Keeps the current remaining marbles unchanged until the bag is exhausted,
	/// then applies the new ratio upon the next refill.
	/// </summary>
	NextCycleDeferred
}
