namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 40: "You gain 2.5% / 4.7% / 7.0% / 9.3% more Health from Stamina."
/// Not modeled yet (reported, no effect).
/// </summary>
public class HeartOfStone : UnmodeledTrait
{
    public const int TraitId = 40;
    public const string TraitName = "Heart of Stone";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
