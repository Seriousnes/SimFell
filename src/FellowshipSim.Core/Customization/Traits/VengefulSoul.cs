namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 5: "Your critical strikes have a chance to increase your Main Stat by 3.0% / 5.1% / 7.2% / 9.3% for 8 seconds. If you are below 50% health when Vengeful Soul triggers, you are also healed for an amount equal to 370% / 630% / 890% / 1150% of your Main Stat over 6 seconds. (3.0 PPM)"
/// Not modeled yet (reported, no effect).
/// </summary>
public class VengefulSoul : UnmodeledTrait
{
    public const int TraitId = 5;
    public const string TraitName = "Vengeful Soul";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
