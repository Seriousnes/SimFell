namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 28: "Your Damage Effects have a chance (2.1 PPM) to burn the target, dealing 483% / 882% / 1281% / 1680% of your Main Stat in fire damage to them over 9 seconds. Your Healing Effects have a chance (2.1 PPM) to cauterize the target, healing them for 483% / 882% / 1281% / 1680% of your Main Stat over 9 seconds."
/// Not modeled yet (reported, no effect).
/// </summary>
public class Kindling : UnmodeledTrait
{
    public const int TraitId = 28;
    public const string TraitName = "Kindling";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
