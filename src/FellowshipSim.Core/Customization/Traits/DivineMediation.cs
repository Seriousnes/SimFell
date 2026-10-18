namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 37: "Taking damage has a chance to Heal you for 332% / 572% / 816% / 1056% Main Stat over 6 seconds. Once the heal expires, you apply an Absorb Shield on yourself and all allies equal to 240% / 416% / 592% / 768% Main Stat. Damage dealt to the shield is reduced by 50%. (1.0 PPM)"
/// Not modeled yet (reported, no effect).
/// </summary>
public class DivineMediation : UnmodeledTrait
{
    public const int TraitId = 37;
    public const string TraitName = "Divine Mediation";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
