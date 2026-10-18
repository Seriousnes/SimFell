namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 38: "When you are healed, you have a chance to gain stacks of Latent Resurgence, consumed to heal you when your health dips below 50%." Defensive or utility: no effect on damage.</summary>
public class LatentResurgence : NoDamageTrait
{
    public const int TraitId = 38;
    public const string TraitName = "Latent Resurgence";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
