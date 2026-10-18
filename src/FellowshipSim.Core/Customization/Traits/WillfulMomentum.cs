namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 39: "Your Spirit Rating is increased by 7 / 11 / 17 / 23. Whenever you trigger a Spirit Refund your Main Stat is increased by 1.5% / 2.6% / 3.7% / 4.8% for 4 seconds."
/// Not modeled yet (reported, no effect).
/// </summary>
public class WillfulMomentum : UnmodeledTrait
{
    public const int TraitId = 39;
    public const string TraitName = "Willful Momentum";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
