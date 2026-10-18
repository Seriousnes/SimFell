namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 37: "Absorbs incoming damage." Defensive or utility: no effect on damage.</summary>
public class DivineMediation : NoDamageTrait
{
    public const int TraitId = 37;
    public const string TraitName = "Divine Mediation";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
