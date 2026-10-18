namespace FellowshipSim.Core.Customization.Sets;

/// <summary>Set bonus power 238: "+4% Critical Strike Chance -15% Threat Generation" (the parts that do not affect damage are left out).</summary>
public class DraconicDeceit : SetBonus
{
    public const string SetName = "Draconic Deceit";
    public const int SetBonusId = 1769;
    public const int SetBonusPowerId = 238;
    public const double CritChanceBonus = 0.04;

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [SetBonusId];

    public override void Bind(Hero hero)
    {
        hero.Stats.BonusCritChance += CritChanceBonus;
    }
}
