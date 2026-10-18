namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 38: "When you are healed, you have a chance to gain 1 stack of Latent Resurgence up to a maximum of 10 stacks. When your health dips below 50% all stacks of Latent Resurgence are consumed, instantly Healing you for an amount equal to 108% / 188% / 268% / 348% of your Main Stat per stack. Latent Resurgence will attempt to save your life if the damage brings you below zero health. (3.3 PPM)"
/// Not modeled yet (reported, no effect).
/// </summary>
public class LatentResurgence : UnmodeledTrait
{
    public const int TraitId = 38;
    public const string TraitName = "Latent Resurgence";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
