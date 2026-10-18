namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 707: "Your BASIC abilities deal an additional 25% / 40% / 65% / 100% Main Stat damage." Added to each hit of
/// a BASIC ability after everything else: no expertise, damage buffs, crit or Frostwyrm's Spite stacks (combat logs:
/// PCaMVTFbZzNLrfx3 fight 5, Frost Bolt non-crits 0.91 and crits 0.77 of the fully scaled model, Frostwyrm's Spite 0.78).
/// </summary>
public class TheVainglorious : Blessing
{
    public const int BlessingId = 707;
    public const string BlessingName = "The Vainglorious";
    public static readonly double[] CoefficientByRank = [0.25, 0.40, 0.65, 1.00];

    public override int Id => BlessingId;
    public override string Name => BlessingName;

    public override void Bind(Hero hero)
    {
        foreach (var ability in hero.Abilities.Where(a => a.Category == AbilityCategory.Basic && a.BaseCoefficient > 0))
        {
            ability.BonusCoefficient += ForRank(CoefficientByRank);
        }
    }
}
