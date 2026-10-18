namespace FellowshipSim.Core.Customization.Sets;

/// <summary>Set bonus power 236: "+4% Haste +20% Movement Speed" (the parts that do not affect damage are left out).</summary>
public class TuzariGrace : SetBonus
{
    public const string SetName = "Tuzari Grace";
    public const int SetBonusId = 680;
    public const int SetBonusPowerId = 236;
    public const double HasteBonus = 0.04;

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [SetBonusId];

    public override void Bind(Hero hero)
    {
        hero.Stats.BonusHaste += HasteBonus;
    }
}
