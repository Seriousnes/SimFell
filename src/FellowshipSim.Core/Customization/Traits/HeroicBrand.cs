namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 50: "Your Weapon Ability Damage and Healing is increased by 24% / 42% / 60% / 80%."
/// Not modeled yet (reported, no effect).
/// </summary>
public class HeroicBrand : UnmodeledTrait
{
    public const int TraitId = 50;
    public const string TraitName = "Heroic Brand";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
