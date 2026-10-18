namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// A blessing (gear rune). Each blessing class lists its game ID, name and per-rank values as constants at the top;
/// <see cref="Bind"/> hooks its effect into the hero.
/// </summary>
public abstract class Blessing
{
    /// <summary>The blessing's game ID. Combat logs give blessings per-item IDs, so logs are matched by <see cref="Name"/>.</summary>
    public abstract int Id { get; }
    public abstract string Name { get; }
    public int Rank { get; init; } = 1;

    /// <summary>False for blessings the engine does not model yet (reported, no effect).</summary>
    public virtual bool IsModeled => true;

    /// <summary>This blessing's value from a per-rank table (rank 1 first).</summary>
    protected double ForRank(IReadOnlyList<double> valuesByRank) => valuesByRank[Math.Clamp(Rank, 1, valuesByRank.Count) - 1];

    public abstract void Bind(Hero hero);
}

/// <summary>A blessing with no engine model yet.</summary>
public abstract class UnmodeledBlessing : Blessing
{
    public override bool IsModeled => false;
    public override void Bind(Hero hero) { }
}
