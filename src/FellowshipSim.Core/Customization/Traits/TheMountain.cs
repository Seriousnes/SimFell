namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 53: "When you take damage that would kill you, your health is set to 1 and you are Immune to damage for 3 seconds (once every 480 seconds)." Defensive or utility: no effect on damage.</summary>
public class TheMountain : NoDamageTrait
{
    public const int TraitId = 53;
    public const string TraitName = "The Mountain";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
