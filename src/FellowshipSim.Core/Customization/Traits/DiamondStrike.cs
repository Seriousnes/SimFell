namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 33: "Your Damage has a chance (6.7 PPM) to conjure a Spectral Beam from above, dealing 72% / 127% / 182% / 237% Main Stat damage to them. Your Heals have a chance (6.7 PPM) to conjure a Spectral Beam from above, healing them for an additional 72% / 127% / 182% / 237% of your Main Stat. Diamond Strike applies an amplifying effect for 20 seconds on targets it strikes, increasing its Damage and Healing to that target by 40%, stacking up to 5 times. Diamond Strike gains an additional +35% increased Damage and Healing for every stack of Harmonious Soul you have active (Diamond Gem Power)."
/// Not modeled yet (reported, no effect).
/// </summary>
public class DiamondStrike : UnmodeledTrait
{
    public const int TraitId = 33;
    public const string TraitName = "Diamond Strike";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
