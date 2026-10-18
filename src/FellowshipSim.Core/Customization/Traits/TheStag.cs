namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 58: "While your defensive ability is active, you gain movement speed and damage reduction." Defensive or utility: no effect on damage.</summary>
public class TheStag : NoDamageTrait
{
    public const int TraitId = 58;
    public const string TraitName = "The Stag";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
