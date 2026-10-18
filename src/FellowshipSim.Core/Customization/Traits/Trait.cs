namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// A gear or weapon-tree trait. Each trait class lists its game ID, name and per-rank values as constants at the top;
/// <see cref="Bind"/> hooks its effect into the hero.
/// </summary>
public abstract class Trait
{
    /// <summary>The trait's game ID (combat logs show it + 3,000,000).</summary>
    public abstract int Id { get; }
    public abstract string Name { get; }
    public int Rank { get; init; } = 1;

    /// <summary>False for traits the engine does not model yet (reported, no effect).</summary>
    public virtual bool IsModeled => true;

    /// <summary>This trait's value from a per-rank table (rank 1 first).</summary>
    protected double ForRank(IReadOnlyList<double> valuesByRank) => valuesByRank[Math.Clamp(Rank, 1, valuesByRank.Count) - 1];

    public abstract void Bind(Hero hero);
}

/// <summary>A trait with no engine model yet (defensive, healing, or not yet implemented).</summary>
public abstract class UnmodeledTrait : Trait
{
    public override bool IsModeled => false;
    public override void Bind(Hero hero) { }
}
