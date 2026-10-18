namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 705: "Your MAJOR abilities gain 3% / 5% / 8% / 12% Cooldown Acceleration. The bonus is increased by +0.1%
/// for every 1% Haste you have, up to a maximum of 5%."
/// </summary>
public class TheMonarch : Blessing
{
    public const int BlessingId = 705;
    public const string BlessingName = "The Monarch";
    public const double HasteBonusPerPercent = 0.001;
    public const double MaxHasteBonus = 0.05;
    public static readonly double[] AccelerationByRank = [0.03, 0.05, 0.08, 0.12];

    public override int Id => BlessingId;
    public override string Name => BlessingName;

    public override void Bind(Hero hero)
    {
        double acceleration = ForRank(AccelerationByRank);
        hero.CooldownAccelerations.Add((ability, now) =>
            ability.Category == AbilityCategory.Major
                ? acceleration + Math.Min(MaxHasteBonus, HasteBonusPerPercent * hero.GetHaste(now) * 100.0)
                : 0.0);
    }
}
