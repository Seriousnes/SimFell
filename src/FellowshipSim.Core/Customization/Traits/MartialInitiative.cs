namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 51: "Your Weapon Ability grants you 10% increased Main Stat and 15% Damage Reduction for a duration equal to
/// 10% / 18% / 25% / 32% of your Weapon Ability's default cooldown."
/// </summary>
public class MartialInitiative : Trait
{
    public const int TraitId = 51;
    public const string TraitName = "Martial Initiative";
    public const int BuffEffectId = 2174;
    public const double BuffPrimaryStat = 1.1;
    public const double BuffDamageTaken = 0.85;
    /// <summary>The buff lasts this fraction of the weapon ability's default cooldown.</summary>
    public static readonly double[] WeaponCooldownFractionByRank = [0.1, 0.18, 0.25, 0.32];

    public static readonly AuraDef Buff = AuraDef.Effect("Martial Initiative", BuffEffectId,
        modifiers: [StatModifier.More(StatType.IncomingDamageMultiplier, BuffDamageTaken), StatModifier.Increased(StatType.PrimaryAttributeMultiplier, BuffPrimaryStat)]);

    public override int Id => TraitId;
    public override string Name => TraitName;

    public override void Bind(Hero hero)
    {
        hero.Register(Buff);
        double fraction = ForRank(WeaponCooldownFractionByRank);
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability != hero.WeaponAbility) return;
            hero.Auras.Apply(Buff.WithDuration(fraction * ability.Cooldown.TotalSeconds), now);
        };
    }
}
