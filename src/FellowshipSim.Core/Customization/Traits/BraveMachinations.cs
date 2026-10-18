namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 48: "Your Weapon Ability's Critical Strike chance is increased by 9.5% / 17% / 24% / 32%. If your Weapon Ability deals a Critical Strike, its Cooldown is reduced by 15% / 20% / 25% / 30%."
/// Not modeled yet (reported, no effect).
/// </summary>
public class BraveMachinations : UnmodeledTrait
{
    public const int TraitId = 48;
    public const string TraitName = "Brave Machinations";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
