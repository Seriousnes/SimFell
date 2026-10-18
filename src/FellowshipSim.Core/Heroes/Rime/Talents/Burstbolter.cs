using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 193: Frost Bolt damage triggers a single Bursting Ice pulse, and Bursting Ice deals more damage.
/// The description says 20% more; combat logs match the datamined constant on Bursting Ice
/// ("Talent.IncreasedCastedDebuffAoeDamageDamage.DamageScaler" = 1.3).
/// </summary>
public class Burstbolter : Talent
{
    public const string DamageScalerConstant = "Talent.IncreasedCastedDebuffAoeDamageDamage.DamageScaler";
    public override string Name => "Burstbolter";
    public override int Id => 193;

    public override void Bind(Hero hero)
    {
        double scaler = PlaceholderConstant(hero, BurstingIce.AbilityId, DamageScalerConstant);
        hero.GetAbility<BurstingIce>()?.AddDamageMultiplier(scaler);
    }

    internal static double PlaceholderConstant(Hero hero, int abilityId, string constant)
    {
        foreach (var p in hero.Data.Raw("ability", abilityId).GetProperty("placeholders").EnumerateArray())
        {
            if (p.TryGetProperty("constants", out var constants) && constants.TryGetProperty(constant, out var value))
            {
                return value.GetDouble();
            }
        }
        throw new KeyNotFoundException($"Ability {abilityId} has no placeholder constant {constant}");
    }
}
