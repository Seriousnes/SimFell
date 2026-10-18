namespace FellowshipSim.Core.Customization.Sets;

/// <summary>"Your Spirit Ability grants you Drakheim's Absolution, increasing your Intellect by 20% for 20 seconds."</summary>
public class DrakheimsAbsolution : SetBonus
{
    public const string SetName = "Drakheim's Absolution";
    public const int AgilitySetBonusId = 690;
    public const int IntellectSetBonusId = 691;
    public const int StrengthSetBonusId = 692;
    public const int EffectId = 1993;
    public const double DurationSeconds = 20.0;
    public const double PrimaryStatBonus = 1.2;

    public static readonly AuraDef Buff = AuraDef.Effect(SetName, EffectId, DurationSeconds,
        modifiers: StatModifier.More(StatType.PrimaryAttribute, PrimaryStatBonus));

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [AgilitySetBonusId, IntellectSetBonusId, StrengthSetBonusId];

    public override void Bind(Hero hero)
    {
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (ability.Category == AbilityCategory.Spirit) hero.Auras.Apply(Buff, now);
        };
    }
}
