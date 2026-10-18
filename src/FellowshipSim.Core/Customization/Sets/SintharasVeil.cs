namespace FellowshipSim.Core.Customization.Sets;

/// <summary>Set bonus power 230: "+4% Spirit +10% magic damage reduction" (the parts that do not affect damage are left out).</summary>
public class SintharasVeil : SetBonus
{
    public const string SetName = "Sinthara's Veil";
    public const int SetBonusId = 689;
    public const int SetBonusPowerId = 230;
    public const double SpiritBonus = 0.04;

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [SetBonusId];

    public override void Bind(Hero hero)
    {
        hero.Stats.BonusSpirit += SpiritBonus;
    }
}
