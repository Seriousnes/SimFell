namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 63: "Your defensive ability heals you and grants damage reduction for 12 seconds." Defensive or utility: no effect on damage.</summary>
public class TheWolf : NoDamageTrait
{
    public const int TraitId = 63;
    public const string TraitName = "The Wolf";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
