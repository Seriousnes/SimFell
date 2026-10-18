namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 35: "Your targeted, offensive abilities have a chance to spawn a Ruby Storm that travels forward, dealing damage to all enemies it touches equal to 2.4% / 4.0% / 5.4% / 7.0% of your Current Health. (1.3 PPM)"
/// Not modeled yet (reported, no effect).
/// </summary>
public class RubyStorm : UnmodeledTrait
{
    public const int TraitId = 35;
    public const string TraitName = "Ruby Storm";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
