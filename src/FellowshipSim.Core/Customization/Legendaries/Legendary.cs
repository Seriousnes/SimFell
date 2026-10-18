namespace FellowshipSim.Core.Customization.Legendaries;

public abstract class Legendary
{
    /// <summary>The legendary power's game ID.</summary>
    public abstract int LegendaryPowerId { get; }
    public abstract string Name { get; }

    /// <summary>The hero whose legendary items grant this power.</summary>
    public abstract string HeroName { get; }

    /// <summary>The item IDs that grant it (the same item ID grants a different power on each hero).</summary>
    public abstract IReadOnlyList<int> ItemIds { get; }

    /// <summary>False for legendaries the engine does not model yet (reported, no effect).</summary>
    public virtual bool IsModeled => true;

    public abstract void Bind(Hero hero);
}

