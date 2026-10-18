using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 190 / effect 1392 ("DamageIncreaseAndCastTimeIncrease"): Glacial Blast deals 40% more damage, cast +0.5s.
/// Talon Strike benefits from talents that affect Glacial Blast (talent 658).
/// </summary>
public class GreaterGlacialBlast : Talent
{
    public const int TalentId = 190;
    public const string TalentName = "Greater Glacial Blast";
    public const double DamageMultiplier = 1.4;
    public const double CastTimeIncreaseSeconds = 0.5;

    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        if (hero.GetAbility<GlacialBlast>() is { } glacialBlast)
        {
            glacialBlast.AddDamageMultiplier(DamageMultiplier);
            glacialBlast.ExtraCastTimeSeconds += CastTimeIncreaseSeconds;
        }
        hero.GetAbility<TalonStrike>()?.AddDamageMultiplier(DamageMultiplier);
    }
}
