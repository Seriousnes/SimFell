namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 50: "Your Weapon Ability Damage and Healing is increased by 24% / 42% / 60% / 80%." (effect 2138)</summary>
public class HeroicBrand : Trait
{
    public const int TraitId = 50;
    public const string TraitName = "Heroic Brand";
    public static readonly double[] DamageIncreaseByRank = [0.24, 0.42, 0.60, 0.80];

    public override int Id => TraitId;
    public override string Name => TraitName;

    public override void Bind(Hero hero)
    {
        double multiplier = 1.0 + ForRank(DamageIncreaseByRank);
        hero.AbilityDamageMultipliers.Add((ability, _) => ability == hero.WeaponAbility ? multiplier : 1.0);
    }
}
