namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 56: "You take 20% reduced Area of Effect damage." Defensive or utility: no effect on damage.</summary>
public class TheLake : NoDamageTrait
{
    public const int TraitId = 56;
    public const string TraitName = "The Lake";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
