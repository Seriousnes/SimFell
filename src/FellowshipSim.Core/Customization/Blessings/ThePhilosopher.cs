using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 709 / effect 3189: [MAJOR] abilities grant +0.2% Haste, Expertise and Critical Strike per X% Spirit
/// (X by rank), up to 50% Spirit, for 8 seconds.
/// </summary>
public class ThePhilosopher : Blessing
{
    public const int EffectId = 3189;
    public const double DurationSeconds = 8.0;
    /// <summary>
    /// Not in the datamine; measured from logs: a refresh keeps up to 30% (2.4s) of the time left, so a refreshed buff
    /// lasts up to 10.4s (yMAZVJL9hTpFjqbC fight 7: every refresh with over 2.4s left is removed 10.42s later).
    /// </summary>
    public const double RefreshCarryoverFraction = 0.3;
    public override string Name => "The Philosopher";

    public override void Bind(Hero hero)
    {
        var baseAura = hero.DefineAura(EffectId).WithDuration(DurationSeconds) with { RefreshCarryoverFraction = RefreshCarryoverFraction };
        double increase = RankData.EffectConstant(hero.Data, EffectId, "IncreasePerSpirit", Rank);
        double threshold = RankData.EffectConstant(hero.Data, EffectId, "SpiritThreshold", Rank);
        double spiritPerIncrease = RankData.EffectConstant(hero.Data, EffectId, "CurrentSpiritPerIncrease", Rank);
        // Applied once the MAJOR ability resolves, so its own effects (e.g. Winter's Blessing's Spirit) count.
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability.Category != "Major") return;
            double spiritPercent = Math.Min(hero.SpiritPercent(now), threshold) * 100.0;
            double bonus = increase * spiritPercent / spiritPerIncrease;
            hero.Auras.Apply(baseAura.WithModifiers(
                StatModifier.Add(StatType.Haste, bonus),
                StatModifier.Add(StatType.Expertise, bonus),
                StatModifier.Add(StatType.CritChance, bonus)), now);
        };
    }
}
