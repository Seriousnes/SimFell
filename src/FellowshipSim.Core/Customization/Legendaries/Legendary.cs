namespace FellowshipSim.Core.Customization.Legendaries;

public abstract class Legendary
{
    /// <summary>The legendary power's game ID.</summary>
    public abstract int LegendaryPowerId { get; }
    public abstract string Name { get; }

    /// <summary>False for legendaries the engine does not model yet (reported, no effect).</summary>
    public virtual bool IsModeled => true;

    public abstract void Bind(Hero hero);
}

