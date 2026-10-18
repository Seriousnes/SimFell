namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 45: "Every 1 second that you stand still, you gain 1 stack of Grounded Spirit up to 10 stacks. Upon moving, all stacks of Grounded Spirit are consumed, healing you and 1 other ally with low health for 60% / 116% / 172% / 228% Main Stat per stack consumed."
/// Not modeled yet (reported, no effect).
/// </summary>
public class GroundedSpirit : UnmodeledTrait
{
    public const int TraitId = 45;
    public const string TraitName = "Grounded Spirit";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
