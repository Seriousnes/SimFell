namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 41: "Every 4 seconds in combat that you do not take damage, you gain 1 stack of Stalwart Readiness. The next time you take any damage it is Reduced by 2% / 3% / 4.5% / 6% for each stack of Stalwart Readiness you have and 1 stack is removed. You can have a maximum of 3 stacks of Stalwart Readiness."
/// Not modeled yet (reported, no effect).
/// </summary>
public class StalwartReadiness : UnmodeledTrait
{
    public const int TraitId = 41;
    public const string TraitName = "Stalwart Readiness";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
