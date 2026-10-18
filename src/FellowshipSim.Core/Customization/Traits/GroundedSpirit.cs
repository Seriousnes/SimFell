namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 45: "Standing still builds stacks that heal you and an ally when you move." Defensive or utility: no effect on damage.</summary>
public class GroundedSpirit : NoDamageTrait
{
    public const int TraitId = 45;
    public const string TraitName = "Grounded Spirit";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
