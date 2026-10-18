using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Sets;

/// <summary>
/// Set bonus 1771: +4% Spirit (in the character sheet's Spirit when imported from a log) and +10% damage
/// (effect 1984, DamageScale x1.1) to "Low Health" enemies: at or below 30% health (not in the datamine; confirmed by logs).
/// </summary>
public class DeathsGrasp : SetBonus
{
    public const double SpiritBonus = 0.04;
    public const int EffectId = 1984;
    public const double LowHealthThreshold = 0.30;
    public override string Name => "Death's Grasp";

    public override void Bind(Hero hero)
    {
        hero.Stats.BonusSpirit += SpiritBonus;
        double multiplier = AuraDef.FromEffect(hero.Data, EffectId).Modifiers
            .Where(m => m.Type == StatType.DamageScale).Select(m => m.Value).DefaultIfEmpty(1.0).First();
        hero.TargetDamageMultipliers.Add((target, _) => target.HealthFraction <= LowHealthThreshold ? multiplier : 1.0);
    }
}
