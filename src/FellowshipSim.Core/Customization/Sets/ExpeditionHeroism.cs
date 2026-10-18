namespace FellowshipSim.Core.Customization.Sets;

/// <summary>Set bonus power 227: "+15% Critical Chance +20% Haste +20% Expertise +20% Spirit" (the parts that do not affect damage are left out).</summary>
public class ExpeditionHeroism : SetBonus
{
    public const string SetName = "Expedition Heroism";
    public const int SetBonusId = 2098;
    public const int SetBonusPowerId = 227;
    public const double CritChanceBonus = 0.15;
    public const double HasteBonus = 0.2;
    public const double ExpertiseBonus = 0.2;
    public const double SpiritBonus = 0.2;

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [SetBonusId];

    public override void Bind(Hero hero)
    {
        hero.Stats.BonusCritChance += CritChanceBonus;
        hero.Stats.BonusHaste += HasteBonus;
        hero.Stats.BonusExpertise += ExpertiseBonus;
        hero.Stats.BonusSpirit += SpiritBonus;
    }
}
