using Fellowship.SDK.Records;
using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Engine;

namespace FellowshipSim.Core.Domain;

public abstract class Ability
{
    public int Id { get; }
    public string Name { get; protected set; }
    public Hero Owner { get; }
    public double BaseCoefficient { get; protected set; }
    public SimTime CastTime { get; protected set; }
    public SimTime Cooldown { get; protected set; }
    public int MaxCharges { get; protected set; } = 1;
    public bool IgnoresGcd { get; protected set; } = false;
    public int WinterOrbCost { get; protected set; } = 0;

    /// <summary>The ID combat logs use for this ability's damage (the ability ID unless it deals damage through an effect).</summary>
    public int DamageLogId { get; protected set; }

    /// <summary>The ability's datamine category (Power, Major, Core, Basic...), used by effects that key off it.</summary>
    public string? Category { get; protected set; }

    /// <summary>Talent and gear multipliers on this ability's damage.</summary>
    public double DamageMultiplier { get; protected set; } = 1.0;

    /// <summary>Extra crit chance on this ability's hits.</summary>
    public double CritChanceBonus { get; set; } = 0.0;

    public bool ScalesWithExpertise { get; protected set; } = true;

    /// <summary>Cooldown recovers faster with haste ("Cooldown Acceleration equal to your Haste").</summary>
    /// <summary>The cooldown is divided by (1 + haste) once, when it starts (datamine scalesWithHaste, single charge).</summary>
    public bool CooldownScalesWithHaste { get; protected set; } = false;

    /// <summary>
    /// Charges recharge with "Cooldown Acceleration equal to your Haste": haste adds to the recovery rate while recharging
    /// (datamine scalesWithHaste on an ability with charges).
    /// </summary>
    public bool ChargesRechargeWithHaste { get; protected set; } = false;

    /// <summary>Weapon abilities run outside cooldown recovery and haste (only cooldown reduction and weapon-specific effects).</summary>
    public bool IgnoresCooldownRecovery => Category == "Weapon";

    /// <summary>Log IDs of the ability's own auras whose cooldown recovery speeds up its own cooldown too.</summary>
    public HashSet<int> OwnRecoveryAuraLogIds { get; } = new();

    /// <summary>Channeled abilities tick for <see cref="ChannelDuration"/> after their cast resolves.</summary>
    public bool IsChanneled { get; protected set; } = false;
    public SimTime ChannelDuration { get; protected set; } = SimTime.Zero;
    /// <summary>Unhasted seconds between channel ticks; haste at channel start shortens it.</summary>
    public double ChannelTickPeriodSeconds { get; protected set; } = 1.0;
    public bool ChannelTicksOnStart { get; protected set; } = false;
    public bool ChannelPartialFinalTick { get; protected set; } = false;

    /// <summary>Unhasted seconds between ticks of the channel starting now (abilities whose tick rate a buff changes override this).</summary>
    public virtual double ChannelTickPeriodFor(SimulationContext ctx) => ChannelTickPeriodSeconds;

    /// <summary>
    /// Multi-target damage scaling: when a hit lands on more than this many targets, each takes sqrt(T/N) of the damage
    /// (0 = no scaling). The datamine names such thresholds ("DamageScalingTargetCountThreshold") but not Rime's values.
    /// </summary>
    public double AoeTargetThreshold { get; protected set; } = 0.0;

    /// <summary>True when the ability's hits keep the damage they had on release instead of reading stats when they land.</summary>
    public bool SnapshotsDamage { get; protected set; }

    protected Ability(Hero owner, int id, string name, double baseCoefficient, double castTimeSeconds = 0.0, double cooldownSeconds = 0.0)
    {
        Owner = owner;
        Id = id;
        DamageLogId = id;
        Name = name;
        BaseCoefficient = baseCoefficient;
        CastTime = SimTime.FromSeconds(castTimeSeconds);
        Cooldown = SimTime.FromSeconds(cooldownSeconds);
    }

    /// <summary>Builds the ability from its datamine record: name, cast/channel timing, cooldown, charges and coefficient.</summary>
    protected Ability(Hero owner, int abilityId)
        : this(owner, abilityId, string.Empty, 0.0)
    {
        var record = owner.Data.Ability(abilityId);
        Name = record.Name;
        Category = record.Category?.ToString();
        BaseCoefficient = CoefficientOf(record);
        CastTime = SimTime.FromSeconds(record.CastTime ?? 0.0);
        Cooldown = SimTime.FromSeconds(record.Cooldown ?? 0.0);
        MaxCharges = (int)(record.Charges ?? 1);
        // The datamine's scalesWithHaste on an ability with a cooldown: charge abilities recharge with haste added to the
        // recovery rate; others have their cooldown divided by haste when it starts. Weapon abilities ignore haste.
        bool hasted = Cooldown > SimTime.Zero && Category != "Weapon"
            && owner.Data.Raw("ability", abilityId).TryGetProperty("scalesWithHaste", out var flag) && flag.ValueKind == System.Text.Json.JsonValueKind.True;
        ChargesRechargeWithHaste = hasted && MaxCharges > 1;
        CooldownScalesWithHaste = hasted && MaxCharges <= 1;
        if (record.ChannelTime is > 0)
        {
            IsChanneled = true;
            ChannelDuration = SimTime.FromSeconds(record.ChannelTime.Value);
            ChannelTickPeriodSeconds = record.ChannelTick ?? record.ChannelTime.Value;
        }
    }

    /// <summary>The first damage coefficient in the ability's placeholders or coefficients.</summary>
    public static double CoefficientOf(AbilityRecord record)
    {
        foreach (var p in record.Placeholders ?? [])
        {
            if (p.Coefficient is > 0) return p.Coefficient.Value;
        }
        foreach (var c in record.Coefficients ?? [])
        {
            if (c.Value > 0 && c.Part is null or "Damage") return c.Value;
        }
        return 0.0;
    }

    public void AddDamageMultiplier(double multiplier) => DamageMultiplier *= multiplier;
    public void ResetDamageMultiplier() => DamageMultiplier = 1.0;

    public bool IsInstant => CastTime == SimTime.Zero;

    public virtual SimTime GetAdjustedCastTime(SimTime now = default)
    {
        if (IsInstant) return SimTime.Zero;
        return SimTime.FromSeconds(CastTime.TotalSeconds / (1.0 + Owner.GetHaste(now)));
    }

    public bool IsReady(SimTime now) => Owner.IsCooldownReady(Name, now);

    /// <summary>
    /// Whether the ability can be cast right now. The default checks charges/cooldown;
    /// abilities with resource costs or other requirements add their own checks.
    /// </summary>
    public virtual bool CanCast(SimulationContext ctx, out string? reason)
    {
        if (Cooldown > SimTime.Zero && Owner.CooldownOf(this).ChargesAt(ctx.CurrentTime) == 0)
        {
            reason = $"{Name} on cooldown ({Owner.GetCooldownRemains(Name, ctx.CurrentTime):F2}s remaining)";
            return false;
        }
        reason = null;
        return true;
    }

    /// <summary>Spends a charge and starts its recharge.</summary>
    public virtual void TriggerCooldown(SimulationContext ctx, double cooldownMultiplier = 1.0)
    {
        if (Cooldown <= SimTime.Zero) return;
        // Cooldown reduction cuts the timer once, at cast; hasted cooldowns are divided by haste at cast too.
        // Cooldown recovery (and haste for charge abilities) then sets how fast it ticks down.
        double seconds = Cooldown.TotalSeconds * cooldownMultiplier * (1.0 - Owner.Stats.CooldownReductionPct);
        if (CooldownScalesWithHaste) seconds /= 1.0 + Owner.GetHaste(ctx.CurrentTime);
        var state = Owner.CooldownOf(this);
        if (state.ChargesAt(ctx.CurrentTime) == 1 && state.LastChargeAt > SimTime.Zero
            && Environment.GetEnvironmentVariable("REPLAY_SLACK") is { } slackFile)
        {
            File.AppendAllText(slackFile, $"{Name}\t{ctx.CurrentTime.TotalSeconds:F3}\t{(ctx.CurrentTime - state.LastChargeAt).TotalSeconds:F3}\n");
        }
        state.Spend(ctx.CurrentTime, seconds);
    }

    /// <summary>The ability's effects when its cast resolves (immediately for instants, at cast completion otherwise).</summary>
    public abstract void Execute(SimulationContext ctx, EnemyTarget target);

    /// <summary>One channel tick. <paramref name="fraction"/> is below 1.0 for a partial final tick.</summary>
    public virtual void OnChannelTick(SimulationContext ctx, EnemyTarget target, double fraction) { }

    /// <summary>The channel ended, naturally or because it was stopped early (<paramref name="clipped"/>).</summary>
    public virtual void OnChannelEnd(SimulationContext ctx, EnemyTarget target, bool clipped) { }

    /// <summary>Damage of one hit before crit and spread.</summary>
    public virtual double BaseDamage(SimulationContext ctx, EnemyTarget? target, double damageMultiplier = 1.0)
    {
        var now = ctx.CurrentTime;
        double expertiseAmp = ScalesWithExpertise ? 1.0 + Owner.Expertise(now) : 1.0;
        return BaseCoefficient
               * Owner.PrimaryAttribute(now)
               * expertiseAmp
               * DamageScaleFor(ctx)
               * Owner.DamageMultiplierAgainst(target, now)
               * Owner.AbilityDamageMultiplier(this, now)
               * DamageMultiplier
               * damageMultiplier;
    }

    /// <summary>Outgoing damage multiplier from auras for this ability (abilities excluded from some auras override this).</summary>
    protected virtual double DamageScaleFor(SimulationContext ctx) => Owner.DamageScale(ctx.CurrentTime);

    /// <param name="extraCritChance">Crit chance this hit alone gets (e.g. from a buff the cast consumed).</param>
    public virtual double DealDamage(SimulationContext ctx, EnemyTarget target, double damageMultiplier = 1.0, bool canCrit = true, string? overrideName = null, int? overrideId = null, bool? forcedCrit = null,
        Action<SimulationContext, double>? onLanded = null, double extraCritChance = 0.0)
    {
        // Damage, crit chance and crit power are read when the hit lands.
        return ctx.ApplyDamage(Owner, target, overrideId ?? DamageLogId, overrideName ?? Name, canCrit,
            c => new SimulationContext.HitNumbers(
                BaseDamage(c, target, damageMultiplier),
                Owner.CritChanceAgainst(target, c.CurrentTime) + CritChanceBonus + extraCritChance,
                Owner.CritMultiplier(c.CurrentTime)),
            forcedCrit, onLanded, SnapshotsDamage);
    }

    public virtual double DealAoeDamage(SimulationContext ctx, IEnumerable<EnemyTarget> targets, double damageMultiplier = 1.0, bool canCrit = true, string? overrideName = null, int? overrideId = null, bool? forcedCrit = null,
        double extraCritChance = 0.0)
    {
        var targetList = ctx.Outcomes.SelectTargets(overrideId ?? DamageLogId, targets.ToList(), ctx);
        if (targetList.Count == 0) return 0.0;

        double falloffMultiplier = 1.0;
        if (AoeTargetThreshold > 0.0 && targetList.Count > AoeTargetThreshold)
        {
            falloffMultiplier = Math.Sqrt(AoeTargetThreshold / targetList.Count);
        }

        double total = 0.0;
        foreach (var t in targetList)
        {
            total += DealDamage(ctx, t, damageMultiplier * falloffMultiplier, canCrit, overrideName, overrideId, forcedCrit, extraCritChance: extraCritChance);
        }
        return total;
    }
}
