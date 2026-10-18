namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 46: "Standing still for 3 seconds grants you Patient Soul, increasing your Max Health by 5% and your Expertise rating by 12 / 22 / 32 / 42. Patient Soul persists for 6 seconds after you start moving."
/// Not modeled yet (reported, no effect).
/// </summary>
public class PatientSoul : UnmodeledTrait
{
    public const int TraitId = 46;
    public const string TraitName = "Patient Soul";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
