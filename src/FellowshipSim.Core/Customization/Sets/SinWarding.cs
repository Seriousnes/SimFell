namespace FellowshipSim.Core.Customization.Sets;

/// <summary>Set bonus power 233: "+4% Expertise +5% Max Health" (the parts that do not affect damage are left out).</summary>
public class SinWarding : SetBonus
{
    public const string SetName = "Sin Warding";
    public const int AgilitySetBonusId = 682;
    public const int IntellectSetBonusId = 683;
    public const int StrengthSetBonusId = 684;
    public const int SetBonusPowerId = 233;
    public const double ExpertiseBonus = 0.04;

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [AgilitySetBonusId, IntellectSetBonusId, StrengthSetBonusId];

    public override void Bind(Hero hero)
    {
        hero.Stats.BonusExpertise += ExpertiseBonus;
    }
}
