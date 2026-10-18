namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 54: "Your Magic Damage Reduction is increased by 10%." Defensive or utility: no effect on damage.</summary>
public class TheShadow : NoDamageTrait
{
    public const int TraitId = 54;
    public const string TraitName = "The Shadow";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
