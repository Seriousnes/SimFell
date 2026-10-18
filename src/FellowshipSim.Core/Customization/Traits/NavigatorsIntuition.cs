namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 43: "Your offensive abilities have a 20% chance to grant you 28 / 66 / 104 / 142 Critical Strike, Haste, Expertise or Spirit rating for 30 seconds. Your highest secondary stat is always the one that gets increased. Navigator's Intuition can trigger once every 90 seconds."
/// Not modeled yet (reported, no effect).
/// </summary>
public class NavigatorsIntuition : UnmodeledTrait
{
    public const int TraitId = 43;
    public const string TraitName = "Navigator's Intuition";
    public override int Id => TraitId;
    public override string Name => TraitName;
}
