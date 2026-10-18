namespace FellowshipSim.Core.Customization.Sets;

/// <summary>Set bonus power 231: "+4% Spirit +15% Max Mana" (the parts that do not affect damage are left out).</summary>
public class HauntingLament : SetBonus
{
    public const string SetName = "Haunting Lament";
    public const int SetBonusId = 1772;
    public const int SetBonusPowerId = 231;
    public const double SpiritBonus = 0.04;

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [SetBonusId];

    public override void Bind(Hero hero)
    {
        hero.Stats.BonusSpirit += SpiritBonus;
    }
}
