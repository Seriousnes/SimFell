namespace FellowshipSim.Core.Customization.RelicPassives;

/// <summary>
/// Relic passive 778 (Orb of Awakened Gaze): "Your critical strikes have a chance (0.9 PPM) to increase your Primary Stat
/// by 18% for 14 seconds." (ability 2326 with a 5 second cooldown, effect 3268 Draconic Might)
/// </summary>
public class DragonboneDice : RelicPassive
{
    public const int RelicPassiveId = 778;
    public const string RelicPassiveName = "Dragonbone Dice";
    public const int OrbItemId = 5925;
    public const int BuffEffectId = 3268;
    public const double BuffSeconds = 14.0;
    public const double MainStatMultiplier = 1.18;
    public const double ProcsPerMinute = 0.9;
    public const double InternalCooldownSeconds = 5.0;
    public const string ProcKey = "dragonbone_dice";

    public static readonly AuraDef Buff = AuraDef.Effect("Draconic Might", BuffEffectId, BuffSeconds,
        modifiers: StatModifier.More(StatType.PrimaryAttributeMultiplier, MainStatMultiplier));

    public override int Id => RelicPassiveId;
    public override string Name => RelicPassiveName;
    public override IReadOnlyList<int> ItemIds => [OrbItemId];

    public override void Bind(Hero hero)
    {
        var rppm = new RealPpm(ProcKey, ProcsPerMinute);
        var ready = SimTime.Zero;
        // The fight the internal cooldown belongs to (each sim iteration and replay has its own).
        SimulationContext? fight = null;
        hero.OnDamageDealt += (in dmg) =>
        {
            if (!dmg.IsCrit || dmg.SimContext is not { } ctx) return;
            if (fight != ctx)
            {
                fight = ctx;
                ready = SimTime.Zero;
            }
            if (ctx.CurrentTime < ready || !rppm.Roll(hero, ctx)) return;
            ready = ctx.CurrentTime + SimTime.FromSeconds(InternalCooldownSeconds);
            hero.Auras.Apply(Buff, ctx.CurrentTime);
        };
    }
}
