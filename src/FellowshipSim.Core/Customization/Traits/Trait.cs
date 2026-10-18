using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;

namespace FellowshipSim.Core.Customization.Traits;

public abstract class Trait
{
    public abstract string Name { get; }
    public int Rank { get; init; } = 1;

    /// <summary>False for traits the engine does not model yet (reported, no effect).</summary>
    public virtual bool IsModeled => true;

    public virtual void Initialize(ICodexDataService codex) { }

    /// <summary>This trait's value at its rank (datamine "trait" record rankRuns).</summary>
    protected double RankValue(Hero hero, int run = 0) => RankData.TraitValue(hero.Data, Name, Rank, run);

    public abstract void Bind(Hero hero);
}

/// <summary>A trait with no engine model yet (defensive, healing, or not yet implemented).</summary>
public abstract class UnmodeledTrait : Trait
{
    public override bool IsModeled => false;
    public override void Bind(Hero hero) { }
}
