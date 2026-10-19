namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 702: "Your [POWER] abilities have a 5% / 8% / 13% / 20% chance to generate 1 Spirit Point toward your SPIRIT
/// ability." UNVERIFIED: no log checked.
/// </summary>
public class TheCelestial : Blessing
{
    public const int BlessingId = 702;
    public const string BlessingName = "The Celestial";
    public static readonly double[] ChanceByRank = [0.05, 0.08, 0.13, 0.20];

    public override int Id => BlessingId;
    public override string Name => BlessingName;

    public const string ProcKey = "the_celestial";
    public const double SpiritPointsGained = 1.0;

    public override void Bind(Hero hero)
    {
        double chance = ForRank(ChanceByRank);
        hero.OnCastCompleted += (ability, _, _) =>
        {
            if (ability.Category == AbilityCategory.Power && hero.SimContext is { } ctx && ctx.Outcomes.RollProc(ProcKey, chance, ctx))
            {
                hero.GainResource(Hero.SpiritPoints, SpiritPointsGained, ctx);
            }
        };
    }
}
