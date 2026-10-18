namespace FellowshipSim.Core.Customization.Blessings;

/// <summary>
/// Blessing 703: "Every 60 seconds you gain The Wayfarer, granting you +6% / +10% / +16% / +25% Haste for 14 seconds. Each
/// time you use your [CORE] ability, the time to The Wayfarer's next application is reduced by 0.2 to 8 seconds, relative
/// to the cooldown of the [CORE] ability used. Abilities with no cooldown provide 0.2 seconds reduction." (buff 3184,
/// cooldown marker 3183). UNVERIFIED: no log has this blessing. The first application is at the start of the fight, and a
/// [CORE] ability takes off its cooldown x 8/60 seconds (0.2 to 8).
/// </summary>
public class TheWayfarer : Blessing
{
    public const int BlessingId = 703;
    public const string BlessingName = "The Wayfarer";
    public const int BuffEffectId = 3184;
    public const double CooldownSeconds = 60.0;
    public const double BuffSeconds = 14.0;
    public const double MinReductionSeconds = 0.2;
    public const double MaxReductionSeconds = 8.0;
    public static readonly double[] HasteByRank = [0.06, 0.10, 0.16, 0.25];

    public static readonly AuraDef Buff = AuraDef.Effect(BlessingName, BuffEffectId, BuffSeconds);

    public override int Id => BlessingId;
    public override string Name => BlessingName;

    public override void Bind(Hero hero)
    {
        var buff = Buff.WithModifiers(StatModifier.Add(StatType.Haste, ForRank(HasteByRank)));
        SimulationContext? fight = null;
        var next = SimTime.Zero;

        void Arm(SimulationContext ctx)
        {
            var at = next;
            ctx.ScheduleAt(at, c =>
            {
                if (at != next) return;
                hero.Auras.Apply(buff, c.CurrentTime);
                next = c.CurrentTime + SimTime.FromSeconds(CooldownSeconds);
                Arm(c);
            }, BlessingName);
        }

        hero.OnCastStarted += (ability, _, now) =>
        {
            if (hero.SimContext is not { } ctx) return;
            if (fight != ctx)
            {
                fight = ctx;
                next = now;
                Arm(ctx);
            }
            if (ability.Category != AbilityCategory.Core) return;
            double reduction = Math.Clamp(ability.Cooldown.TotalSeconds * MaxReductionSeconds / CooldownSeconds, MinReductionSeconds, MaxReductionSeconds);
            next = SimTime.FromSeconds(Math.Max(now.TotalSeconds, next.TotalSeconds - reduction));
            Arm(ctx);
        };
    }
}
