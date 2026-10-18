namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 44: "When you use a Relic Ability, all damage you take is reduced by 4.7% / 7.8% / 10.9% / 14% for 12 seconds." Defensive or utility: no effect on damage.</summary>
public class TreasureHuntersDelight : NoDamageTrait
{
    public const int TraitId = 44;
    public const string TraitName = "Treasure Hunter's Delight";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
