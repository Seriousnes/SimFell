using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 190 / effect 1392 ("DamageIncreaseAndCastTimeIncrease"): Glacial Blast deals 40% more damage, cast +0.5s.
/// Talon Strike benefits from talents that affect Glacial Blast (talent 658).
/// </summary>
public class GreaterGlacialBlast : Talent
{
    public const double CastTimeIncreaseSeconds = 0.5;
    public override string Name => "Greater Glacial Blast";
    public override int Id => 190;

    public override void Bind(Hero hero)
    {
        double multiplier = hero.Data.Raw("effect", 1392).GetProperty("talents")
            .GetProperty("DamageIncreaseAndCastTimeIncrease").GetProperty("AdditionalDamage").GetDouble();
        hero.GetAbility<GlacialBlast>()?.AddDamageMultiplier(multiplier);
        hero.GetAbility<TalonStrike>()?.AddDamageMultiplier(multiplier);
    }
}
