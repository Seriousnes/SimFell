namespace FellowshipSim.Core.Customization.Sets;

/// <summary>
/// "+5% Intellect. When dipping below 20% Health, you are instantly healed for 40% of your maximum health." The main stat
/// is already in the character sheet's Intellect when imported from a log (applied otherwise); the heal is defensive.
/// </summary>
public class TormentOfBaelAurum : SetBonus
{
    public const string SetName = "Torment of Bael'Aurum";
    public const int AgilitySetBonusId = 685;
    public const int IntellectSetBonusId = 686;
    public const int StrengthSetBonusId = 687;
    public const int EffectId = 1988;
    public const double PrimaryStatBonus = 1.05;

    public static readonly AuraDef Bonus = AuraDef.Effect(SetName, EffectId,
        modifiers: StatModifier.More(StatType.PrimaryAttribute, PrimaryStatBonus));

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [AgilitySetBonusId, IntellectSetBonusId, StrengthSetBonusId];
    public override IReadOnlyList<string> UnmodeledParts => ["low-health heal"];

    public override void Bind(Hero hero)
    {
        if (!hero.Stats.UsesSheetValues) hero.PassiveAuras.Add(Bonus);
    }
}
