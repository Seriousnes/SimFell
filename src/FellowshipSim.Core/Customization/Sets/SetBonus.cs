namespace FellowshipSim.Core.Customization.Sets;

/// <summary>
/// A gear set's 2-piece bonus. Each set class lists its set-bonus IDs (one per main-stat variant of the set) and values
/// as constants at the top; <see cref="Bind"/> hooks its effect into the hero.
/// </summary>
public abstract class SetBonus
{
    public abstract string Name { get; }

    /// <summary>The set-bonus IDs of this set (combat logs show them on each gear piece).</summary>
    public abstract IReadOnlyList<int> SetBonusIds { get; }

    /// <summary>Parts of the bonus the engine does not model yet (reported).</summary>
    public virtual IReadOnlyList<string> UnmodeledParts => Array.Empty<string>();

    public abstract void Bind(Hero hero);
}
