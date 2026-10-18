namespace FellowshipSim.Core.Customization.Sets;

/// <summary>
/// Set bonus power 629: "Your Gem Power is increased by 20%." A build's gem powers are the game's totals (combat logs
/// report them with this bonus included), so it adds nothing on top.
/// </summary>
public class SealOfTheHeskyr : SetBonus
{
    public const string SetName = "Seal of the Heskyr";
    public const int SetBonusId = 5638;
    public const int SetBonusPowerId = 629;
    public const double GemPowerBonus = 0.20;

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [SetBonusId];
    public override void Bind(Hero hero) { }
}
