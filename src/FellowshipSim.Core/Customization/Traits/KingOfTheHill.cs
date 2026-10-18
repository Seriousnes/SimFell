namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 6: "You take 3% / 6% / 9% / 12% Reduced Damage while you are above 80% health." Defensive or utility: no effect on damage.</summary>
public class KingOfTheHill : NoDamageTrait
{
    public const int TraitId = 6;
    public const string TraitName = "King of the Hill";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
