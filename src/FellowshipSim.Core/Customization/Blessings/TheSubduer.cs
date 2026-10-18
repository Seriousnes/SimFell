namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 704: "You deal 5% / 10% / 15% / 20% more damage to Low Health Enemies. Your [MAJOR] abilities reduce the
/// cooldown of one of your [CONTROL] abilities ..." (effect 3185; the Control cooldowns do not affect damage).
/// UNVERIFIED: no log has this blessing; low health is taken as 30% or less, like Death's Grasp.
/// </summary>
public class TheSubduer : Blessing
{
    public const int BlessingId = 704;
    public const string BlessingName = "The Subduer";
    public const double LowHealthThreshold = 0.30;
    public static readonly double[] DamageByRank = [0.05, 0.10, 0.15, 0.20];

    public override int Id => BlessingId;
    public override string Name => BlessingName;

    public override void Bind(Hero hero)
    {
        double multiplier = 1.0 + ForRank(DamageByRank);
        hero.TargetDamageMultipliers.Add((target, _) => target.HealthFraction <= LowHealthThreshold ? multiplier : 1.0);
    }
}
