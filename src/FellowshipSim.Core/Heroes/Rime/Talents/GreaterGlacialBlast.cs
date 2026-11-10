using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 190 / effect 1392 ("DamageIncreaseAndCastTimeIncrease"): Glacial Blast deals more damage and has a longer cast.
/// Talon Strike benefits from talents that affect Glacial Blast (talent 658).
/// </summary>
public partial class GreaterGlacialBlast : Talent
{
    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        if (hero.GetAbility<GlacialBlast>() is { } glacialBlast)
        {
            glacialBlast.AddDamageMultiplier(DAMAGE_INCREASE_AND_CAST_TIME_INCREASE_ADDITIONAL_DAMAGE);
            glacialBlast.ExtraCastTimeSeconds += DAMAGE_INCREASE_AND_CAST_TIME_INCREASE_CAST_TIME_INCREASE_DURATION;
        }
        hero.GetAbility<TalonStrike>()?.AddDamageMultiplier(DAMAGE_INCREASE_AND_CAST_TIME_INCREASE_ADDITIONAL_DAMAGE);
    }
}
