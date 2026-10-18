namespace FellowshipSim.Core.Customization.Sets;

/// <summary>"Your abilities have a chance (0.8 PPM) to increase your Haste by 25% for 20 seconds." Rolled on every ability used.</summary>
public class DarkProphecy : SetBonus
{
    public const string SetName = "Dark Prophecy";
    public const int SetBonusId = 681;
    public const int SetBonusPowerId = 246;
    public const int EffectId = 1994;
    public const double ProcsPerMinute = 0.8;
    public const double DurationSeconds = 20.0;
    public const double HasteBonus = 0.25;
    public const string ProcKey = "dark_prophecy";

    public static readonly AuraDef Buff = AuraDef.Effect(SetName, EffectId, DurationSeconds,
        modifiers: StatModifier.Add(StatType.Haste, HasteBonus));

    public override string Name => SetName;
    public override IReadOnlyList<int> SetBonusIds => [SetBonusId];

    public override void Bind(Hero hero)
    {
        var rppm = new RealPpm(ProcKey, ProcsPerMinute);
        hero.OnCastCompleted += (ability, _, now) =>
        {
            if (hero.SimContext is { } ctx && rppm.Roll(hero, ctx, Buff.LogId)) hero.Auras.Apply(Buff, now);
        };
    }
}
