namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 47: "Your Weapon Ability's Cooldown is reduced by 25% / 50% / 75% / 100% when you activate your Spirit Ability."
/// (a fraction of its default cooldown). The Spirit Points it generates do not affect damage.
/// </summary>
public class VisionsOfGrandeur : Trait
{
    public const int TraitId = 47;
    public const string TraitName = "Visions of Grandeur";
    public static readonly double[] WeaponCooldownFractionByRank = [0.25, 0.5, 0.75, 1.0];

    public override int Id => TraitId;
    public override string Name => TraitName;

    public override void Bind(Hero hero)
    {
        double fraction = ForRank(WeaponCooldownFractionByRank);
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability.Category != AbilityCategory.Spirit || hero.WeaponAbility == null) return;
            hero.ReduceCooldown(hero.WeaponAbility.Name, SimTime.FromSeconds(fraction * hero.WeaponAbility.Cooldown.TotalSeconds), now);
        };
    }
}
