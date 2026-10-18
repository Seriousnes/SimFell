using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 193: Frost Bolt damage triggers a single Bursting Ice pulse, and Bursting Ice deals more damage.
/// The description says 20% more; combat logs match the game data's constant on Bursting Ice
/// ("Talent.IncreasedCastedDebuffAoeDamageDamage.DamageScaler" = 1.3).
/// </summary>
public class Burstbolter : Talent
{
    public const int TalentId = 193;
    public const string TalentName = "Burstbolter";
    /// <summary>Bursting Ice damage multiplier.</summary>
    public const double BurstingIceDamage = 1.3;
    public override string Name => TalentName;
    public override int Id => TalentId;

    public override void Bind(Hero hero)
    {
        hero.GetAbility<BurstingIce>()?.AddDamageMultiplier(BurstingIceDamage);
    }
}
