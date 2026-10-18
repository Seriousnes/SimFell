namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 59: "Your defensive ability's duration is increased by 4 seconds (Rime: Frost Ward, 4s to 8s in PCaMVTFbZzNLrfx3 fight 5)." Defensive or utility: no effect on damage.</summary>
public class TheTower : NoDamageTrait
{
    public const int TraitId = 59;
    public const string TraitName = "The Tower";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
