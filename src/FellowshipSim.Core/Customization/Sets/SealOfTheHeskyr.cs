namespace FellowshipSim.Core.Customization.Sets;

/// <summary>"Your Gem Power is increased by 25%." Already in the gem powers a combat log reports.</summary>
public class SealOfTheHeskyr : SetBonus
{
    public const string SetName = "Seal of the Heskyr";
    public const int SetBonusId = 5638;
    public const double GemPowerBonus = 0.25;

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [SetBonusId];
    public override void Bind(Hero hero) { }
}
