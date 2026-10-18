namespace FellowshipSim.Core.Customization.Sets;

/// <summary>
/// "+4% Spirit. +10% damage to Low Health enemies." The Spirit is in the character sheet's Spirit when imported from a
/// log. "Low Health" is at or below 30% health (not in the game data; confirmed by logs). Damage: effect 1984.
/// </summary>
public class DeathsGrasp : SetBonus
{
    public const string SetName = "Death's Grasp";
    public const int SetBonusId = 1771;
    public const int EffectId = 1984;
    public const double SpiritBonus = 0.04;
    public const double LowHealthDamage = 1.1;
    public const double LowHealthThreshold = 0.30;

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [SetBonusId];

    public override void Bind(Hero hero)
    {
        hero.Stats.BonusSpirit += SpiritBonus;
        hero.TargetDamageMultipliers.Add((target, _) => target.HealthFraction <= LowHealthThreshold ? LowHealthDamage : 1.0);
    }
}
