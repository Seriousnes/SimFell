namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 706: "Your POWER abilities gain 0.3% / 0.5% / 0.8% / 1.2% increased power for every 3% Spirit you have, up
/// to 50% Spirit."
/// </summary>
public class TheMystic : Blessing
{
    public const int BlessingId = 706;
    public const string BlessingName = "The Mystic";
    public const double SpiritPercentPerStep = 3.0;
    public const double SpiritCap = 0.5;
    public static readonly double[] PowerPerStepByRank = [0.003, 0.005, 0.008, 0.012];

    public override int Id => BlessingId;
    public override string Name => BlessingName;

    public override void Bind(Hero hero)
    {
        double perStep = ForRank(PowerPerStepByRank);
        hero.AbilityDamageMultipliers.Add((ability, now) =>
            ability.Category == AbilityCategory.Power
                ? 1.0 + perStep * (Math.Min(hero.SpiritPercent(now), SpiritCap) * 100.0 / SpiritPercentPerStep)
                : 1.0);
    }
}
