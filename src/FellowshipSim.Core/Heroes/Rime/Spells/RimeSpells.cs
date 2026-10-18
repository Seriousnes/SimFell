using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Spells;

// Values come from the datamine records named in each class (ability IDs unless noted as effects).

/// <summary>Base for Rime's abilities, built from a datamine ability record.</summary>
public abstract class RimeAbility : Ability
{
    protected RimeAbility(Rime owner, int abilityId) : base(owner, abilityId) { }
    protected RimeAbility(Rime owner, int id, string name, double coefficient) : base(owner, id, name, coefficient) { }

    protected Rime Rime => (Rime)Owner;
}

/// <summary>Ability 1020. 2 charges, cooldown accelerated by haste, generates 1 Winter Orb.</summary>
public class ColdSnap : RimeAbility
{
    public const int AbilityId = 1020;
    public const int WinterOrbsGained = 1;

    public ColdSnap(Rime owner) : base(owner, AbilityId)
    {
        // "Cold Snap gains Cooldown Acceleration equal to your Haste": its charges recharge with haste added to the rate.
        ChargesRechargeWithHaste = true;
        CooldownScalesWithHaste = false;
    }

    public override bool CanCast(SimulationContext ctx, out string? reason)
    {
        // Navir's Keeper: "causes your next Cold Snap to not require nor cost any charge"
        if (Rime.Auras.HasAura(Rime.Aura.NavirsKeeper, ctx.CurrentTime))
        {
            reason = null;
            return true;
        }
        return base.CanCast(ctx, out reason);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        if (Environment.GetEnvironmentVariable("REPLAY_TRACE_COLDSNAP") != null)
            Console.Error.WriteLine($"TRACE coldsnap @ {ctx.CurrentTime}: charges {hero.CooldownOf(this).ChargesAt(ctx.CurrentTime)} keeper {hero.Auras.GetStacks(hero.Aura.NavirsKeeper, ctx.CurrentTime)} next {hero.CooldownOf(this).NextChargeAt} haste {hero.GetHaste(ctx.CurrentTime):F3}");
        if (hero.Auras.HasAura(hero.Aura.NavirsKeeper, ctx.CurrentTime))
        {
            hero.Auras.ConsumeStack(hero.Aura.NavirsKeeper, ctx.CurrentTime);
        }
        else
        {
            TriggerCooldown(ctx);
        }

        hero.GenerateWinterOrbs(WinterOrbsGained, ctx, target);
        if (hero.HasTalent<GlacialAssault>())
        {
            hero.Auras.Apply(hero.Aura.GlacialAssault, ctx.CurrentTime);
        }
        if (hero.HasTalent<IcyFlow>())
        {
            hero.Auras.Apply(hero.Aura.IcyFlow, ctx.CurrentTime);
        }
        if (hero.HasTalent<ChillingFinesse>())
        {
            hero.ReduceCooldown<FreezingTorrent>(SimTime.FromSeconds(ChillingFinesse.FreezingTorrentReductionSeconds), ctx.CurrentTime);
        }

        DealDamage(ctx, target);
        hero.TriggerFrostSwallows(ctx, target);
    }
}

/// <summary>Shared rules of Glacial Blast and Talon Strike (Talon Strike benefits from talents that affect Glacial Blast).</summary>
public abstract class GlacialBlastFamily : RimeAbility
{
    /// <summary>Talent 38 Glacial Assault: "deals 40% more damage" at 4 stacks.</summary>
    public const double GlacialAssaultMultiplier = 1.40;
    /// <summary>Talent 38: "dealing 30% of the initial damage to nearby enemies" (ability 2023's text says 20%).</summary>
    public const double GlacialAssaultExplosionFraction = 0.30;
    public const int GlacialAssaultExplosionId = 2023;
    /// <summary>
    /// Combat logs: the explosion is 30% of the hit actually dealt (crit included), never crits itself, and loses damage
    /// as sqrt(12/N) beyond 12 targets (yMAZVJL9hTpFjqbC fight 7 @ 31.8s: explosion/spike = 0.2598 on 16, 0.2520 on 17).
    /// </summary>
    public const double GlacialAssaultExplosionTargetThreshold = 12;

    protected GlacialBlastFamily(Rime owner, int abilityId) : base(owner, abilityId) { }

    protected bool GlacialAssaultReady => Rime.GlacialAssaultStacks >= 4;

    /// <summary>Called when a Glacial Assault hit on <paramref name="target"/> lands, with the damage it dealt.</summary>
    protected void Explode(SimulationContext ctx, EnemyTarget target, double initialDamage)
    {
        if (initialDamage <= 0.0) return;
        var nearby = ctx.Outcomes.SelectTargets(GlacialAssaultExplosionId, ctx.ActiveEnemies.Where(e => e != target).ToList(), ctx);
        if (nearby.Count == 0) return;
        double falloff = nearby.Count > GlacialAssaultExplosionTargetThreshold ? Math.Sqrt(GlacialAssaultExplosionTargetThreshold / nearby.Count) : 1.0;
        double damage = initialDamage * GlacialAssaultExplosionFraction * falloff;
        foreach (var enemy in nearby)
        {
            ctx.ApplyDamage(Owner, enemy, damage, GlacialAssaultExplosionId, "Glacial Assault", canCrit: false, 0.0, 1.0);
        }
    }
}

/// <summary>Ability 1028. Costs 2 Winter Orbs.</summary>
public class GlacialBlast : GlacialBlastFamily
{
    public const int AbilityId = 1028;
    public const int DefaultWinterOrbCost = 2;

    public GlacialBlast(Rime owner) : base(owner, AbilityId)
    {
        WinterOrbCost = DefaultWinterOrbCost;
    }

    public override SimTime GetAdjustedCastTime(SimTime now = default)
    {
        // Instant with 4 Glacial Assault stacks or during Wrath of Winter.
        if (GlacialAssaultReady || Rime.Auras.HasAura(Rime.Aura.WrathOfWinter, now)) return SimTime.Zero;
        double seconds = CastTime.TotalSeconds + (Rime.HasTalent<GreaterGlacialBlast>() ? GreaterGlacialBlast.CastTimeIncreaseSeconds : 0.0);
        // Icy Flow: "+50% Haste to you while casting" the Glacial Blast that consumes it.
        double icyFlowHaste = Rime.Auras.HasAura(Rime.Aura.IcyFlow, now) ? IcyFlow.CastHaste : 0.0;
        return SimTime.FromSeconds(seconds / (1.0 + Owner.GetHaste(now) + icyFlowHaste));
    }

    public override bool CanCast(SimulationContext ctx, out string? reason)
    {
        if (Rime.HasTalent<IcyTalons>())
        {
            reason = $"{Name} is replaced by Talon Strike (Icy Talons)";
            return false;
        }
        if (Rime.WinterOrbs < DefaultWinterOrbCost && !GlacialAssaultReady)
        {
            reason = $"{Name} needs {DefaultWinterOrbCost} Winter Orbs (have {Rime.WinterOrbs})";
            return false;
        }
        return base.CanCast(ctx, out reason);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        bool assault = GlacialAssaultReady;
        double multiplier = 1.0;
        if (assault)
        {
            hero.Auras.Remove(hero.Aura.GlacialAssault, ctx.CurrentTime);
            multiplier = GlacialAssaultMultiplier;
        }
        else
        {
            hero.SpendWinterOrbs(DefaultWinterOrbCost, ctx);
        }

        double crit = hero.ConsumeSpenderCritBuffs(ctx);
        DealDamage(ctx, target, damageMultiplier: multiplier, onLanded: assault ? (c, dealt) => Explode(c, target, dealt) : null, extraCritChance: crit);
    }
}

/// <summary>Ability 2223 (Icy Talons). One ice spike per Winter Orb spent; costs all Winter Orbs.</summary>
public class TalonStrike : GlacialBlastFamily
{
    public const int AbilityId = 2223;

    public TalonStrike(Rime owner) : base(owner, AbilityId) { }

    public override bool CanCast(SimulationContext ctx, out string? reason)
    {
        if (!Rime.HasTalent<IcyTalons>())
        {
            reason = $"{Name} needs Icy Talons";
            return false;
        }
        if (Rime.WinterOrbs < 1 && !GlacialAssaultReady)
        {
            reason = $"{Name} needs a Winter Orb";
            return false;
        }
        return base.CanCast(ctx, out reason);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        bool assault = GlacialAssaultReady;
        // With Glacial Assault it is free and fires as if cast with full Winter Orbs (per the user; Rising Talons never
        // consumes Glacial Assault in the logs).
        int spikes = assault ? Rime.MaxWinterOrbs : Math.Max(1, hero.WinterOrbs);
        double multiplier = 1.0;
        if (assault)
        {
            // Glacial Assault: no cost and 40% more damage.
            hero.Auras.Remove(hero.Aura.GlacialAssault, ctx.CurrentTime);
            multiplier = GlacialAssaultMultiplier;
            // INFERRED from logs (not the tooltip): cast "as if" with full Winter Orbs, it counts them as spent for Wisdom of
            // the North. Across 10 logs this removes 46 of 62 Ice Blitz cooldown misses while some casts still land right
            // as Ice Blitz comes back (so it is not too fast).
            if (hero.HasTalent<WisdomOfTheNorth>()) WisdomOfTheNorth.ReduceCooldowns(hero, spikes, ctx.CurrentTime);
        }
        else
        {
            hero.SpendWinterOrbs(spikes, ctx);
        }

        // Talon Strike benefits from talents that affect Glacial Blast: every spike of the cast gets the consumed crit buffs.
        double crit = hero.ConsumeSpenderCritBuffs(ctx);
        for (int i = 0; i < spikes; i++)
        {
            DealDamage(ctx, target, damageMultiplier: multiplier, onLanded: assault ? (c, dealt) => Explode(c, target, dealt) : null, extraCritChance: crit);
        }
    }
}

/// <summary>Ability 2224 (Icy Talons). Ice spikes per Winter Orb on the target and all nearby enemies; costs all Winter Orbs.</summary>
public class RisingTalons : RimeAbility
{
    public const int AbilityId = 2224;

    /// <summary>Combat logs: each spike loses damage as sqrt(12/N) beyond 12 targets.</summary>
    public const double TargetThreshold = 12;

    /// <summary>
    /// Combat logs (10 Rime logs, 626 casts): one wave of spikes per Winter Orb; the first erupts 0.30-0.40s after the cast
    /// (server frames; 25th percentile 0.301s, median 0.334s), then 4-5 server frames apart regardless of haste (median
    /// 0.1447s). Each wave hits the enemies present when it erupts. The earliest usual times are used (0.30s, 4 frames),
    /// so a wave is never released after the moment it should land.
    /// </summary>
    public const double FirstWaveDelaySeconds = 0.30;
    public const double WaveIntervalSeconds = 4.0 / 30.0;

    public RisingTalons(Rime owner) : base(owner, AbilityId)
    {
        AoeTargetThreshold = TargetThreshold;
    }

    public override bool CanCast(SimulationContext ctx, out string? reason)
    {
        if (!Rime.HasTalent<IcyTalons>())
        {
            reason = $"{Name} needs Icy Talons";
            return false;
        }
        if (Rime.WinterOrbs < 1)
        {
            reason = $"{Name} needs a Winter Orb";
            return false;
        }
        return base.CanCast(ctx, out reason);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        int spikes = Math.Max(1, Rime.WinterOrbs);
        Rime.SpendWinterOrbs(spikes, ctx);

        // Combat logs: every wave keeps the damage of the cast, even if buffs drop before it lands
        // (yMAZVJL9hTpFjqbC fight 7 @ 168.5, 207.4, 234.1), but hits the enemies present when it erupts
        // (@ 32.358: an enemy appearing after the cast is hit by every wave). The target falloff is fixed at the cast too
        // (PrkThcx8Y3RgVXyq @ 38.61: waves on 19, 7 and 3 enemies deal the same per hit).
        var now = ctx.CurrentTime;
        int enemiesAtCast = Math.Max(1, ctx.ActiveEnemies.Count());
        double falloff = enemiesAtCast > TargetThreshold ? Math.Sqrt(TargetThreshold / enemiesAtCast) : 1.0;
        double damage = BaseDamage(ctx, null) * falloff;
        // Rising Talons benefits from talents that affect Ice Comet: the crit buffs it consumes and Avalanche.
        double critChance = Owner.CritChance(now) + CritChanceBonus + Rime.ConsumeSpenderCritBuffs(ctx);
        double critMultiplier = Owner.CritMultiplier(now);
        for (int i = 0; i < spikes; i++)
        {
            ctx.Schedule(SimTime.FromSeconds(FirstWaveDelaySeconds + i * WaveIntervalSeconds),
                c => ReleaseWave(c, target, damage, critChance, critMultiplier), $"{Name} wave {i + 1}");
        }
        Rime.TriggerAvalanche(ctx, target);
    }

    private void ReleaseWave(SimulationContext ctx, EnemyTarget target, double damage, double critChance, double critMultiplier)
    {
        var enemies = ctx.ActiveEnemies.ToList();
        if (enemies.Count == 0 && target is { IsSleeping: false }) enemies.Add(target);
        foreach (var enemy in ctx.Outcomes.SelectTargets(DamageLogId, enemies, ctx))
        {
            ctx.ApplyDamage(Owner, enemy, DamageLogId, Name, canCrit: true,
                c => new SimulationContext.HitNumbers(
                    damage * Owner.DamageMultiplierAgainst(enemy, c.CurrentTime),
                    critChance + Owner.CritChanceAgainst(enemy, c.CurrentTime) - Owner.CritChance(c.CurrentTime),
                    critMultiplier),
                snapshot: true);
        }
    }
}

/// <summary>Ability 1018. Costs 2 Winter Orbs.</summary>
public class IceComet : RimeAbility
{
    public const int AbilityId = 1018;
    public const int DefaultWinterOrbCost = 2;

    /// <summary>hero_data Ice Comet TargetCountThreshold 12: damage falls off as sqrt(12/N) beyond 12 targets.</summary>
    public const double TargetThreshold = 12;

    public IceComet(Rime owner) : base(owner, AbilityId)
    {
        WinterOrbCost = DefaultWinterOrbCost;
        AoeTargetThreshold = TargetThreshold;
    }

    public override bool CanCast(SimulationContext ctx, out string? reason)
    {
        if (Rime.HasTalent<IcyTalons>())
        {
            reason = $"{Name} is replaced by Rising Talons (Icy Talons)";
            return false;
        }
        if (Rime.WinterOrbs < DefaultWinterOrbCost)
        {
            reason = $"{Name} needs {DefaultWinterOrbCost} Winter Orbs (have {Rime.WinterOrbs})";
            return false;
        }
        return base.CanCast(ctx, out reason);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        Rime.SpendWinterOrbs(DefaultWinterOrbCost, ctx);
        Strike(this, ctx, target, Rime.ConsumeSpenderCritBuffs(ctx));
        Rime.TriggerAvalanche(ctx, target);
    }

    /// <summary>One comet impact on <paramref name="target"/> and all enemies around it.</summary>
    internal static void Strike(RimeAbility comet, SimulationContext ctx, EnemyTarget target, double extraCritChance)
    {
        var enemies = ctx.ActiveEnemies.ToList();
        if (enemies.Count == 0 && target is { IsSleeping: false }) enemies.Add(target);
        comet.DealAoeDamage(ctx, enemies, extraCritChance: extraCritChance);
    }
}

/// <summary>
/// Ability 2227 ("Ice Comet", GA_Rime_Util_OnTargetPulsatingAoe): the free comets of Cold Shower and Avalanche. A plain Ice
/// Comet even with Icy Talons (per the user). UNVERIFIED: the damage ID these log under is not in any log we have.
/// </summary>
public class ProcIceComet : RimeAbility
{
    public const int AbilityId = 2227;

    public ProcIceComet(Rime owner) : base(owner, AbilityId)
    {
        AoeTargetThreshold = IceComet.TargetThreshold;
    }

    public void Strike(SimulationContext ctx, EnemyTarget target, double extraCritChance) => IceComet.Strike(this, ctx, target, extraCritChance);

    public override void Execute(SimulationContext ctx, EnemyTarget target) => Strike(ctx, target, 0.0);
}

/// <summary>
/// Ability 1031. Applies debuff effect 1395 (3s, pulsing every 0.5s hasted) whose pulses deal damage effect 1396
/// to the target and nearby enemies and generate 1 Anima. While it is active Rime has Winter's Embrace (effect 2303).
/// </summary>
public class BurstingIce : RimeAbility
{
    public const int AbilityId = 1031;
    public const int PulseEffectId = 1396;
    public const double AnimaPerPulse = 1.0;

    private int _activeDebuffs;

    /// <summary>Combat logs: pulses lose damage as sqrt(5/N) beyond 5 targets.</summary>
    public const double TargetThreshold = 5;

    public BurstingIce(Rime owner) : base(owner, AbilityId)
    {
        AoeTargetThreshold = TargetThreshold;
        DamageLogId = GameData.EffectLogId(PulseEffectId);
        var debuff = owner.Data.Effect(1395);
        DurationSeconds = debuff.Duration ?? 3.0;
        PulsePeriodSeconds = debuff.Period ?? 0.5;
    }

    public double DurationSeconds { get; }
    public double PulsePeriodSeconds { get; }

    /// <summary>One pulse around <paramref name="target"/>; <paramref name="fraction"/> scales a partial final pulse.</summary>
    public void DealPulse(SimulationContext ctx, EnemyTarget target, double fraction = 1.0)
    {
        var hero = Rime;
        hero.AddAnima(AnimaPerPulse, ctx, target);
        var enemies = ctx.ActiveEnemies.ToList();
        if (enemies.Count == 0 && target != null) enemies.Add(target);

        // Harrowing Ice: the pulse deals damage with the stacks held before it, then adds its own stack; the stack after 30
        // consumes them all and shortens Flight of the Navir.
        double harrowing = 1.0;
        bool harrowingIce = hero.HasTalent<HarrowingIce>();
        if (harrowingIce) harrowing += HarrowingIce.DamagePerStack * hero.Auras.GetStacks(hero.Aura.HarrowingIce, ctx.CurrentTime);
        DealAoeDamage(ctx, enemies, damageMultiplier: fraction * harrowing);
        if (harrowingIce)
        {
            var now = ctx.CurrentTime;
            hero.Auras.Apply(hero.Aura.HarrowingIce, now);
            if (hero.Auras.GetStacks(hero.Aura.HarrowingIce, now) >= hero.Aura.HarrowingIce.MaxStacks)
            {
                hero.Auras.Remove(hero.Aura.HarrowingIce, now);
                hero.ReduceCooldown<FlightOfTheNavir>(SimTime.FromSeconds(HarrowingIce.FlightOfTheNavirReductionSeconds), now);
            }
        }
    }

    /// <summary>"Winter's Embrace does not affect Bursting Ice."</summary>
    protected override double DamageScaleFor(SimulationContext ctx)
    {
        double scale = Owner.DamageScale(ctx.CurrentTime);
        if (Rime.Auras.HasAura(Rime.Aura.WintersEmbrace, ctx.CurrentTime))
        {
            foreach (var mod in Rime.Aura.WintersEmbrace.Modifiers)
            {
                if (mod.Type == StatType.DamageScale && mod.Op == ModifierOp.MultiplyCompound) scale /= mod.Value;
            }
        }
        return scale;
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        TriggerCooldown(ctx);

        _activeDebuffs++;
        hero.Auras.Apply(hero.Aura.WintersEmbrace, ctx.CurrentTime);

        PeriodicEffect.BeginHasted(ctx, hero, Name, DurationSeconds, PulsePeriodSeconds, tickOnStart: false, partialFinalTick: true,
            // Combat logs (10 Rime logs, 526 debuffs): the final pulse deals the fraction of a pulse period left when the
            // debuff runs out (leftover 0.4 -> 0.43x, 0.5 -> 0.52x, 0.8 -> 0.78x, 0.9 -> 0.88x of a full pulse).
            onTick: (c, fraction) => DealPulse(c, target, fraction),
            onEnd: (c, _) =>
            {
                if (--_activeDebuffs == 0) hero.Auras.Remove(hero.Aura.WintersEmbrace, c.CurrentTime);
            });
    }

    public void ResetState() => _activeDebuffs = 0;
}

/// <summary>Ability 1027. Channel: ticks every 0.4s (hasted) for 2s, 1 Anima per tick.</summary>
public class FreezingTorrent : RimeAbility
{
    public const int AbilityId = 1027;
    public const double AnimaPerTick = 1.0;

    public FreezingTorrent(Rime owner) : base(owner, AbilityId)
    {
        // Combat logs: a tick lands as the channel starts, then every hasted period, plus a partial final tick.
        ChannelTicksOnStart = true;
        ChannelPartialFinalTick = true;
    }

    /// <summary>Supreme Torrent: the channel lasts longer.</summary>
    public void ExtendChannel(double seconds) => ChannelDuration += SimTime.FromSeconds(seconds);

    private bool _soulfrost;

    /// <summary>Soulfrost Torrent: the channel it empowers ticks 40% faster.</summary>
    public override double ChannelTickPeriodFor(SimulationContext ctx) =>
        _soulfrost ? ChannelTickPeriodSeconds / SoulfrostTorrent.TickRateMultiplier : ChannelTickPeriodSeconds;

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        TriggerCooldown(ctx);
        _soulfrost = Rime.Auras.HasAura(Rime.Aura.SoulfrostTorrent, ctx.CurrentTime);
        if (_soulfrost) Rime.Auras.Remove(Rime.Aura.SoulfrostTorrent, ctx.CurrentTime);
    }

    public override void OnChannelEnd(SimulationContext ctx, EnemyTarget target, bool clipped) => _soulfrost = false;

    public override void OnChannelTick(SimulationContext ctx, EnemyTarget target, double fraction)
    {
        // Flight of the Navir: every tick (the partial one too) sends a swallow at an enemy of its choosing
        // (yMAZVJL9hTpFjqbC fight 7 @ 49.522: 10 ticks, 10 hits; @ 34.229: one per tick until Flight of the Navir ends).
        if (Rime.Auras.HasAura(Rime.Aura.FlightOfTheNavir, ctx.CurrentTime))
        {
            var swallows = Rime.GetAbility<CommandedFrostSwallows>();
            var enemy = swallows is not { Available: > 0 } ? null : ctx.Outcomes.ChooseTarget(swallows.DamageLogId, ctx.ActiveEnemies.ToList(), ctx);
            if (enemy != null) swallows!.Send(ctx, enemy);
        }
        Rime.AddAnima(AnimaPerTick, ctx, target);
        DealDamage(ctx, target, damageMultiplier: fraction, extraCritChance: _soulfrost ? SoulfrostTorrent.CritChance : 0.0);
        if (Rime.HasTalent<ColdShower>() && ctx.Outcomes.RollProc(ColdShower.ProcKey, ColdShower.ProcChance, ctx))
        {
            Rime.CastFreeIceComet(ctx, target);
        }
        if (Rime.HasTalent<ChillingFinesse>())
        {
            Rime.ReduceCooldown<BurstingIce>(SimTime.FromSeconds(ChillingFinesse.BurstingIceReductionSeconds), ctx.CurrentTime);
        }
    }
}

/// <summary>Ability 1029. Generates 3 Anima.</summary>
public class FrostBolt : RimeAbility
{
    public const int AbilityId = 1029;
    public const double AnimaGain = 3.0;

    public FrostBolt(Rime owner) : base(owner, AbilityId) { }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        Rime.AddAnima(AnimaGain, ctx, target);
        // Talent 193 Burstbolter: "When your Frost Bolt deals damage to an enemy, it triggers a single pulse of Bursting Ice."
        // (on the bolt's hit, after its travel: yMAZVJL9hTpFjqbC fight 7 @ 57.569 cast, 57.906 hit and pulse)
        var pulse = Rime.HasTalent<Burstbolter>() ? Rime.GetAbility<BurstingIce>() : null;
        DealDamage(ctx, target, onLanded: pulse == null ? null : (c, dealt) => { if (dealt > 0.0) pulse.DealPulse(c, target); });
    }
}

/// <summary>Ability 1032 / effect 1397: +20% damage for 20s, extended 0.1s per Frost Swallow hit. Usable while casting.</summary>
public class IceBlitz : RimeAbility
{
    public const int AbilityId = 1032;
    public const double ExtensionPerSwallowSeconds = 0.1;

    public IceBlitz(Rime owner) : base(owner, AbilityId)
    {
        IgnoresGcd = true;
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        Rime.Auras.Apply(Rime.Aura.IceBlitz, ctx.CurrentTime);
        TriggerCooldown(ctx);
    }
}

/// <summary>Ability 1026 / effects 1389 + 2299: +20% Spirit for 20s. Usable during the global cooldown.</summary>
public class WintersBlessing : RimeAbility
{
    public const int AbilityId = 1026;

    public WintersBlessing(Rime owner) : base(owner, AbilityId)
    {
        IgnoresGcd = true;
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        Rime.Auras.Apply(Rime.Aura.WintersBlessing, ctx.CurrentTime);
        TriggerCooldown(ctx);
    }
}

/// <summary>Ability 1009 / effect 2446: 5 Frost Swallows circle Rime for 20s.</summary>
public class FlightOfTheNavir : RimeAbility
{
    public const int AbilityId = 1009;
    public const int SwallowCount = 5;

    public FlightOfTheNavir(Rime owner) : base(owner, AbilityId) { }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        hero.Auras.Apply(hero.Aura.FlightOfTheNavir, ctx.CurrentTime);
        if (hero.HasTalent<NavirsKeeper>())
        {
            hero.Auras.Apply(hero.Aura.NavirsKeeper, ctx.CurrentTime, setStacks: NavirsKeeper.StacksGranted);
        }
        TriggerCooldown(ctx);
    }
}

/// <summary>
/// Ability 1023 (Spirit ability) / effect 1387: +20% damage for 20s and 1 Winter Orb every 4s; grants
/// Spirit of Heroism (effect 2253, +30% Haste).
/// </summary>
public class WrathOfWinter : RimeAbility
{
    public const int AbilityId = 1023;
    public const double OrbIntervalSeconds = 4.0;

    public WrathOfWinter(Rime owner) : base(owner, AbilityId)
    {
        // Combat logs: cast 0.265s after a Cold Snap, inside its global cooldown (yMAZVJL9hTpFjqbC fight 7 @ 27.284).
        IgnoresGcd = true;
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        var hero = Rime;
        hero.ActivateSpiritOfHeroism(ctx);
        hero.Auras.Apply(hero.Aura.WrathOfWinter, ctx.CurrentTime);

        double duration = hero.Aura.WrathOfWinter.Duration.TotalSeconds;
        // Not in the description, but every log shows it: the first orb comes on activation (yMAZVJL9hTpFjqbC fight 7 @ 709.809: 4 -> 5 orbs).
        // The datamine marks the ability scalesWithHaste: the 4 seconds between orbs are hasted (logs: orbs every ~2.4s at ~69% haste),
        // by the haste when the previous orb came (yMAZVJL9hTpFjqbC fight 7: orbs at ~40.2 and ~42.5 despite The Philosopher
        // lapsing 38.44-39.17 in between).
        PeriodicEffect.BeginHasted(ctx, hero, Name, duration, OrbIntervalSeconds, tickOnStart: true, partialFinalTick: false,
            onTick: (c, _) => hero.GenerateWinterOrbs(1, c), retimeOnHasteChange: false);
        TriggerCooldown(ctx);
    }
}

/// <summary>Shared Frost Swallow hit: scales with Spirit; each hit that lands extends Ice Blitz and may trigger Bursting Swallows.</summary>
public abstract class FrostSwallowAttack : RimeAbility
{
    /// <summary>"For every 1% Spirit you have, your Frost Swallows deal 1.5% more damage."</summary>
    public const double DamagePerSpiritPercent = 1.5;
    public const string BurstingSwallowsProc = "bursting_swallows";

    protected FrostSwallowAttack(Rime owner, int abilityId) : base(owner, abilityId) => owner.OnDamageDealt += OnSwallowHit;
    protected FrostSwallowAttack(Rime owner, int id, string name, double coefficient) : base(owner, id, name, coefficient) => owner.OnDamageDealt += OnSwallowHit;

    public override double BaseDamage(SimulationContext ctx, EnemyTarget? target, double damageMultiplier = 1.0) =>
        base.BaseDamage(ctx, target, damageMultiplier) * (1.0 + DamagePerSpiritPercent * Owner.SpiritPercent(ctx.CurrentTime));

    public void Attack(SimulationContext ctx, EnemyTarget target, int count)
    {
        if (target == null) return;
        for (int i = 0; i < count; i++) DealDamage(ctx, target);
    }

    private void OnSwallowHit(DamageContext dmg)
    {
        if (dmg.AbilityId != DamageLogId || dmg.SimContext == null || dmg.Target == null) return;
        var ctx = dmg.SimContext;
        var hero = Rime;

        // "The duration of Ice Blitz is extended by 0.1 seconds each time a Frost Swallow hits an enemy."
        hero.Auras.Extend(hero.Aura.IceBlitz, SimTime.FromSeconds(IceBlitz.ExtensionPerSwallowSeconds));

        // Talent 628: "Your Frost Swallow hits have a 12% chance to trigger a pulse of Bursting Ice on the target."
        if (hero.HasTalent<BurstingSwallows>() &&
            ctx.Outcomes.RollProc(BurstingSwallowsProc, BurstingSwallows.ProcChance, ctx, GameData.EffectLogId(BurstingIce.PulseEffectId)))
        {
            hero.GetAbility<BurstingIce>()?.DealPulse(ctx, dmg.Target);
        }
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target) => Attack(ctx, target, 1);
}

/// <summary>Ability 1033: "Rime commands Frost Swallows to attack her target." (Cascading Blitz swallows.)</summary>
public class FrostSwallows : FrostSwallowAttack
{
    public const int AbilityId = 1033;

    public FrostSwallows(Rime owner) : base(owner, AbilityId) { }

    /// <summary>Swallows that pick their own targets among the enemies in combat.</summary>
    public void AttackEnemies(SimulationContext ctx, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var target = ctx.Outcomes.ChooseTarget(DamageLogId, ctx.ActiveEnemies.ToList(), ctx);
            if (target != null) Attack(ctx, target, 1);
        }
    }
}

/// <summary>Effect 1365 (part of Flight of the Navir): the circling swallows commanded by Cold Snap and Freezing Torrent.</summary>
public class CommandedFrostSwallows : FrostSwallowAttack
{
    public const int EffectId = 1365;

    public CommandedFrostSwallows(Rime owner)
        : base(owner, GameData.EffectLogId(EffectId), "Frost Swallows", CoefficientOf(owner.Data.Ability(FlightOfTheNavir.AbilityId))) { }

    private int _inFlight;

    /// <summary>
    /// The circling swallows not out attacking: one that is sent comes back when it hits (combat logs: a Cold Snap cast
    /// while a Freezing Torrent swallow is still flying sends only 4, yMAZVJL9hTpFjqbC fight 7 @ 51.956).
    /// </summary>
    public int Available => Math.Max(0, FlightOfTheNavir.SwallowCount - _inFlight);

    /// <summary>Sends one circling swallow at <paramref name="target"/>, if one is available.</summary>
    public bool Send(SimulationContext ctx, EnemyTarget target)
    {
        if (Available == 0) return false;
        _inFlight++;
        DealDamage(ctx, target, onLanded: (_, _) => _inFlight = Math.Max(0, _inFlight - 1));
        return true;
    }

    public void ResetState() => _inFlight = 0;
}

/// <summary>
/// Talent 36 Coalescing Frost: Freezing Torrent hits stack the debuff (effect 2306) on their target; it erupts for 56%
/// Intellect per stack around the enemy when it expires (3s after the last stack) or the enemy dies (damage effect 2300).
/// </summary>
public class CoalescingFrostEruption : RimeAbility
{
    public const string DoubleStackProc = "coalescing_frost_double";

    private readonly Dictionary<EnemyTarget, (int Stacks, int Version)> _stacks = new();
    private SimulationContext? _subscribed;

    public CoalescingFrostEruption(Rime owner)
        : base(owner, GameData.EffectLogId(CoalescingFrost.DamageEffectId), "Coalescing Frost", CoalescingFrost.CoefficientPerStack)
    {
        AoeTargetThreshold = CoalescingFrost.TargetThreshold;
    }

    public void Enable(Hero hero) => hero.OnDamageDealt += OnTorrentHit;

    private void OnTorrentHit(DamageContext dmg)
    {
        var torrent = Rime.GetAbility<FreezingTorrent>();
        if (torrent == null || dmg.AbilityId != torrent.DamageLogId || dmg.Amount <= 0 || dmg.Target == null || dmg.SimContext == null) return;
        var ctx = dmg.SimContext;
        if (_subscribed != ctx)
        {
            ctx.EnemyDemising += (c, enemy) => Erupt(c, enemy, dying: true);
            _subscribed = ctx;
        }

        // "Freezing Torrent critical strikes have a 50% chance to apply 2 stacks."
        int added = dmg.IsCrit && ctx.Outcomes.RollProc(DoubleStackProc, CoalescingFrost.CritDoubleStackChance, ctx) ? 2 : 1;
        var target = dmg.Target;
        var (stacks, version) = _stacks.GetValueOrDefault(target);
        int expected = version + 1;
        _stacks[target] = (stacks + added, expected);
        ctx.Schedule(SimTime.FromSeconds(CoalescingFrost.DurationSeconds), c =>
        {
            if (_stacks.TryGetValue(target, out var current) && current.Version == expected) Erupt(c, target, dying: false);
        }, "Coalescing Frost expires");
    }

    private void Erupt(SimulationContext ctx, EnemyTarget target, bool dying)
    {
        if (!_stacks.Remove(target, out var state) || state.Stacks <= 0) return;
        var enemies = ctx.ActiveEnemies.Where(e => !dying || e != target).ToList();
        if (enemies.Count == 0) return;
        DealAoeDamage(ctx, enemies, damageMultiplier: state.Stacks);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target) { }

    public void ResetState()
    {
        _stacks.Clear();
        _subscribed = null;
    }
}
