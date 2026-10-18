namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 52: "When you take damage equal to 40% of your max health or more in a single hit, the damage is reduced by 30% (once every 20 seconds)." Defensive or utility: no effect on damage.</summary>
public class TheBeetle : NoDamageTrait
{
    public const int TraitId = 52;
    public const string TraitName = "The Beetle";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
