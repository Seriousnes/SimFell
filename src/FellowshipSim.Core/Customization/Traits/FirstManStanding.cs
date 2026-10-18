namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 29: "Each time an enemy deals damage to you for the first time, you gain First Man Standing, reducing all damage you take by 3.0% / 5.2% / 7.4% / 9.6% for 20 seconds."
/// Not modeled yet (reported, no effect).
/// </summary>
public class FirstManStanding : UnmodeledTrait
{
    public const int TraitId = 29;
    public const string TraitName = "First Man Standing";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
