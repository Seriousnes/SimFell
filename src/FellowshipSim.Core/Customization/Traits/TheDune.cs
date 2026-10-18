namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 55: "Your defensive ability further reduces Magic Damage taken while active." Defensive or utility: no effect on damage.</summary>
public class TheDune : NoDamageTrait
{
    public const int TraitId = 55;
    public const string TraitName = "The Dune";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
