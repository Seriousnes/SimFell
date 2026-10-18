namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 41: "Every 4 seconds in combat that you do not take damage, you gain a stack that reduces the next damage you take." Defensive or utility: no effect on damage.</summary>
public class StalwartReadiness : NoDamageTrait
{
    public const int TraitId = 41;
    public const string TraitName = "Stalwart Readiness";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
