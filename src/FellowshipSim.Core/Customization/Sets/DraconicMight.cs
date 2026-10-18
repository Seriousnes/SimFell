namespace FellowshipSim.Core.Customization.Sets;

/// <summary>
/// "Your critical strikes have a chance (0.9 PPM) to increase your Intellect by 18% for 14 seconds."
/// Not modeled yet (reported, no effect).
/// </summary>
public class DraconicMight : SetBonus
{
    public const string SetName = "Draconic Might";
    public const int AgilitySetBonusId = 677;
    public const int IntellectSetBonusId = 678;
    public const int StrengthSetBonusId = 679;

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [AgilitySetBonusId, IntellectSetBonusId, StrengthSetBonusId];
    public override IReadOnlyList<string> UnmodeledParts => ["entire bonus"];
    public override void Bind(Hero hero) { }
}
