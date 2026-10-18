namespace FellowshipSim.Core.Customization.Traits;

/// <summary>
/// Trait 30: "Your abilities have a chance to increase your Cooldown Recovery by 30% / 36% / 42% / 48% for 15 seconds.
/// (1.0 PPM)" (effect 102)
/// </summary>
public class TopazWinds : Trait
{
    public const int TraitId = 30;
    public const string TraitName = "Topaz Winds";
    public const int BuffEffectId = 102;
    public const double BuffSeconds = 15.0;
    public const double ProcsPerMinute = 1.0;
    public const string ProcKey = "topaz_winds";
    public static readonly double[] CooldownRecoveryByRank = [0.30, 0.36, 0.42, 0.48];

    public static readonly AuraDef Buff = AuraDef.Effect("Topaz Winds", BuffEffectId, BuffSeconds);

    public override int Id => TraitId;
    public override string Name => TraitName;

    public override void Bind(Hero hero)
    {
        var buff = Buff.WithModifiers(StatModifier.Increased(StatType.CooldownRecovery, 1.0 + ForRank(CooldownRecoveryByRank)));
        var rppm = new RealPpm(ProcKey, ProcsPerMinute);
        hero.OnCastCompleted += (_, _, now) =>
        {
            if (hero.SimContext is { } ctx && rppm.Roll(hero, ctx)) hero.Auras.Apply(buff, now);
        };
    }
}
