namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 36: "Taking damage has a chance to increase your Armor by 10% / 17% / 24% / 32% for 10 seconds. While this is
/// active, you deal 16% / 29% / 42% / 55% Main Stat physical damage to enemies who deal damage to you. (1.5 PPM)"
/// Not modeled: it reacts to damage taken, which the engine does not simulate (reported, no effect).
/// </summary>
public class IronSpikes : UnmodeledTrait
{
    public const int TraitId = 36;
    public const string TraitName = "Iron Spikes";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
