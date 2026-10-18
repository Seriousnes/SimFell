namespace FellowshipSim.Core.Customization.Traits;

/// <summary>Trait 57: "Your defensive ability causes you to heal for 30% of all damage you deal for 8 seconds." Defensive or utility: no effect on damage.</summary>
public class TheViper : NoDamageTrait
{
    public const int TraitId = 57;
    public const string TraitName = "The Viper";

    public override int Id => TraitId;
    public override string Name => TraitName;
}
