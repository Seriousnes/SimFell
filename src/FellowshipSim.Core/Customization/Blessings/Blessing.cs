using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;

namespace FellowshipSim.Core.Customization.Blessings;

public abstract class Blessing
{
    public abstract string Name { get; }
    public int Rank { get; init; } = 1;

    /// <summary>False for blessings the engine does not model yet (reported, no effect).</summary>
    public virtual bool IsModeled => true;

    public virtual void Initialize(ICodexDataService codex) { }

    /// <summary>The <paramref name="series"/>-th "a / b / c / d" value series of the blessing's description, at this rank.</summary>
    protected double DescriptionValue(Hero hero, int series = 0) =>
        RankData.DescriptionRankValue(RankData.BlessingDescription(hero.Data, Name), Rank, series);

    protected double Constant(Hero hero, string constant) => RankData.BlessingConstant(hero.Data, Name, constant, Rank);

    public abstract void Bind(Hero hero);
}

/// <summary>A blessing with no engine model yet.</summary>
public abstract class UnmodeledBlessing : Blessing
{
    public override bool IsModeled => false;
    public override void Bind(Hero hero) { }
}
