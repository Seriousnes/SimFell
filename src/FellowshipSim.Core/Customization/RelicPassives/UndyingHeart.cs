namespace FellowshipSim.Core.Customization.RelicPassives;

/// <summary>
/// Relic passive 779 (Orb of Gathering Storms): "Your abilities have a chance (0.8 PPM) to increase your Haste by 25% for
/// 20 seconds." (ability 2327 with a 5 second cooldown, effect 3270 Dark Prophecy) Rolled on every ability used.
/// </summary>
public class UndyingHeart : RelicPassive
{
    public const int RelicPassiveId = 779;
    public const string RelicPassiveName = "Undying Heart";
    public const int OrbItemId = 5924;
    public const int EffectId = 3270;
    public const double ProcsPerMinute = 0.8;
    public const double InternalCooldownSeconds = 5.0;
    public const double DurationSeconds = 20.0;
    public const double HasteBonus = 0.25;
    public const string ProcKey = "undying_heart";

    public static readonly AuraDef Buff = AuraDef.Effect("Dark Prophecy", EffectId, DurationSeconds,
        modifiers: StatModifier.Add(StatType.Haste, HasteBonus));

    public override int Id => RelicPassiveId;
    public override string Name => RelicPassiveName;
    public override IReadOnlyList<int> ItemIds => [OrbItemId];

    public override void Bind(Hero hero)
    {
        var rppm = new RealPpm(ProcKey, ProcsPerMinute);
        var ready = SimTime.Zero;
        // The fight the internal cooldown belongs to (each sim iteration and replay has its own).
        SimulationContext? fight = null;
        hero.OnCastCompleted += (_, _, now) =>
        {
            if (hero.SimContext is not { } ctx) return;
            if (fight != ctx)
            {
                fight = ctx;
                ready = SimTime.Zero;
            }
            if (now < ready || !rppm.Roll(hero, ctx)) return;
            ready = now + SimTime.FromSeconds(InternalCooldownSeconds);
            hero.Auras.Apply(Buff, now);
        };
    }
}
