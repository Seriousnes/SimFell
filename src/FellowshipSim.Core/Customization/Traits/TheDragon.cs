namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 61: "Your defensive ability grants you an absorb based on your maximum health." Defensive or utility: no effect on damage.</summary>
public class TheDragon : NoDamageTrait
{
    public const int TraitId = 61;
    public const string TraitName = "The Dragon";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
