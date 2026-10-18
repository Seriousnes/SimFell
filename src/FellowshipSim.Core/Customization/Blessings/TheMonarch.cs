using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>Blessing 705: MAJOR abilities gain Cooldown Acceleration, +0.1% per 1% Haste up to +5%.</summary>
public class TheMonarch : Blessing
{
    public const double HasteBonusPerPercent = 0.001;
    public const double MaxHasteBonus = 0.05;
    public override string Name => "The Monarch";

    public override void Bind(Hero hero)
    {
        double acceleration = DescriptionValue(hero);
        hero.CooldownAccelerations.Add((ability, now) =>
            ability.Category == "Major"
                ? acceleration + Math.Min(MaxHasteBonus, HasteBonusPerPercent * hero.GetHaste(now) * 100.0)
                : 0.0);
    }
}
