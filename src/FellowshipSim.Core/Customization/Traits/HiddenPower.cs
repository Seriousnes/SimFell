namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 1: "Your abilities have a chance to grant you 1 stack of Hidden Power for 60 seconds. When you have 5 stacks, Hidden Power is consumed and Power Revealed is granted to you for 15 seconds, increasing your Main Stat by 7.2% / 12.8% / 18.4% / 24%. You can not gain Hidden Power stacks while Power Revealed is active on you. (2.6 PPM)"
/// Not modeled yet (reported, no effect).
/// </summary>
public class HiddenPower : UnmodeledTrait
{
    public const int TraitId = 1;
    public const string TraitName = "Hidden Power";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
