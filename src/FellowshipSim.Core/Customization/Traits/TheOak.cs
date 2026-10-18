namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 62: "Your Armor is increased by 35%." Defensive or utility: no effect on damage.</summary>
public class TheOak : NoDamageTrait
{
    public const int TraitId = 62;
    public const string TraitName = "The Oak";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
