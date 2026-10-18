namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 2: "Your abilities have a chance to reduce the Cooldown of your Weapon Ability by 1.5 / 4 / 6 / 8 seconds and grant you and 1 / 2 / 3 / 3 allies 6 / 10 / 13 / 17 Haste rating for 8 seconds. (1.2 PPM)"
/// Not modeled for the hero yet (reported, no effect). The Haste buff other players give with it is applied in
/// replays (see Replay/PartyBuffs.cs), scaled to the giver's rank.
/// </summary>
public class InspiredAllegiance : UnmodeledTrait
{
    public const int TraitId = 2;
    public const string TraitName = "Inspired Allegiance";
    public const int BuffEffectId = 2112;
    public static readonly double[] HasteRatingByRank = [6, 10, 13, 17];
    public override int Id => TraitId;
    public override string Name => TraitName;
}
