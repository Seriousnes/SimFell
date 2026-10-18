using FellowshipSim.Core.Customization.Blessings;
using FellowshipSim.Core.Customization.Gems;
using FellowshipSim.Core.Customization.Legendaries;
using FellowshipSim.Core.Customization.Traits;
using FellowshipSim.Core.Customization.Weapons;

namespace FellowshipSim.Core.Domain;

public abstract class Hero : Actor
{
    protected Hero()
    {
        // Spirit of Heroism is applied by every hero's Spirit ability; register it up front so a replay compares it from the start.
        Register(SpiritOfHeroism);
        // Cooldown recovery depends on haste and cooldown acceleration: progress every recharge at the old rate when they change.
        Auras.OnGained += (_, _, now) => AdvanceCooldowns(now);
        Auras.OnRemoved += (_, now) => AdvanceCooldowns(now);
    }

    private void AdvanceCooldowns(SimTime now)
    {
        foreach (var state in Cooldowns.Values) state.Advance(now);
    }

    /// <summary>The simulation the hero is in (for effects triggered by events that carry no context, e.g. casts).</summary>
    public SimulationContext? SimContext { get; internal set; }

    public EnemyTarget CurrentTarget { get; set; } = null!;
    public Weapon? EquippedWeapon { get; set; }
    public Ability? WeaponAbility { get; set; }
    public List<Ability> Abilities { get; } = new();
    public List<Blessing> Blessings { get; } = new();
    public List<Trait> Traits { get; } = new();
    public List<Legendary> Legendaries { get; } = new();
    public List<Gem> Gems { get; } = new();
    public List<Talent> Talents { get; } = new();
    public List<Customization.Sets.SetBonus> Sets { get; } = new();

    public Dictionary<string, CooldownState> Cooldowns { get; } = new(StringComparer.OrdinalIgnoreCase);
    public SimTime GlobalCooldownEndTime { get; set; } = SimTime.Zero;

    /// <summary>The global cooldown before haste (per the user: 1.5s, hasted).</summary>
    public double GlobalCooldownSeconds { get; set; } = 1.5;

    /// <summary>Parts of the hero's build the engine does not model (filled when the build is imported).</summary>
    public List<string> UnmodeledFeatures { get; } = new();

    /// <summary>Permanent auras (passive talents, gear) applied at the start of every combat.</summary>
    public List<AuraDef> PassiveAuras { get; } = new();

    /// <summary>
    /// The date of the game being simulated (a replayed fight's date), for balance changes that came in on a known day;
    /// null simulates the current game.
    /// </summary>
    public DateTime? GameDate { get; set; }

    /// <summary>
    /// A hero-wide damage tuning aura applied by the game server (balance changes that are not in the datamine);
    /// each hero declares its own in its hero file. <paramref name="history"/> lists each value with the date it took
    /// effect; the one in force on <see cref="GameDate"/> applies (none before the first).
    /// </summary>
    protected void AddClassTuningAura(string name, params (DateTime From, double DamageMultiplier)[] history)
    {
        var date = GameDate ?? DateTime.MaxValue;
        double multiplier = history.Where(h => h.From <= date).OrderBy(h => h.From).Select(h => h.DamageMultiplier).LastOrDefault(1.0);
        if (multiplier != 1.0) PassiveAuras.Add(AuraDef.Internal(name, 0, 1, StatModifier.More(StatType.DamageScale, multiplier)));
    }

    /// <summary>Log IDs of every aura this hero's abilities and customizations can apply.</summary>
    public HashSet<int> ModeledAuraLogIds { get; } = new();

    /// <summary>Extra crit chance against a specific target (e.g. bonuses vs. healthy enemies).</summary>
    public List<Func<EnemyTarget, SimTime, double>> TargetCritChanceBonuses { get; } = new();

    /// <summary>Damage multipliers against a specific target (e.g. bonuses vs. low-health enemies).</summary>
    public List<Func<EnemyTarget, SimTime, double>> TargetDamageMultipliers { get; } = new();

    /// <summary>Damage multipliers for specific abilities that depend on the moment (e.g. scaling with current Spirit).</summary>
    public List<Func<Ability, SimTime, double>> AbilityDamageMultipliers { get; } = new();

    /// <summary>Cooldown acceleration for specific abilities (0.05 = cooldown recovers 5% faster).</summary>
    public List<Func<Ability, SimTime, double>> CooldownAccelerations { get; } = new();

    /// <summary>Seconds added to Spirit of Heroism.</summary>
    public double SpiritOfHeroismDurationBonus { get; set; }

    /// <summary>Auras active exactly while Spirit of Heroism is active.</summary>
    public List<AuraDef> SpiritOfHeroismAuras { get; } = new();

    /// <summary>Spirit of Heroism (effect 2253, granted by every hero's Spirit ability): +30% Haste.</summary>
    public const int SpiritOfHeroismEffectId = 2253;
    public const double SpiritOfHeroismSeconds = 20.0;
    public const double SpiritOfHeroismHaste = 0.3;

    /// <summary>Spirit of Heroism: every hero's Spirit ability grants +30% Haste for 20 seconds.</summary>
    public static readonly AuraDef SpiritOfHeroism = AuraDef.Effect("Spirit of Heroism", SpiritOfHeroismEffectId, SpiritOfHeroismSeconds,
        modifiers: StatModifier.Add(StatType.Haste, SpiritOfHeroismHaste));

    public event Action<DamageContext>? OnDamageDealt;
    public event Action<Ability, EnemyTarget?, SimTime>? OnCastStarted;
    public event Action<Ability, EnemyTarget?, SimTime>? OnCastCompleted;
    public event Action<Ability, EnemyTarget?, SimTime>? OnCastCancelled;
    public event Action<Ability, EnemyTarget?, SimTime, bool>? OnChannelEnded;
    /// <summary>A channel ticked (after the ability's own tick effects); the double is the tick's fraction (below 1 for a partial tick).</summary>
    public event Action<Ability, EnemyTarget?, double, SimulationContext>? OnChannelTick;
    /// <summary>Resource was spent (or counts as spent).</summary>
    public event Action<ResourceDef, double, SimTime>? OnResourceSpent;
    /// <summary>Resource was gained past its maximum: the double is the amount that overflowed.</summary>
    public event Action<ResourceDef, double, SimTime>? OnResourceOverflow;

    /// <summary>The cast in progress, if any (abilities with a cast time between begin and completion).</summary>
    public ActiveCast? CurrentCast { get; internal set; }

    /// <summary>The channel in progress, if any.</summary>
    public ActiveChannel? CurrentChannel { get; internal set; }

    public void TriggerDamageDealt(DamageContext ctx) => OnDamageDealt?.Invoke(ctx);
    public void TriggerCastStarted(Ability a, EnemyTarget? t, SimTime now = default) => OnCastStarted?.Invoke(a, t, now);
    public void TriggerCastCompleted(Ability a, EnemyTarget? t, SimTime now = default) => OnCastCompleted?.Invoke(a, t, now);
    public void TriggerCastCancelled(Ability a, EnemyTarget? t, SimTime now = default) => OnCastCancelled?.Invoke(a, t, now);
    public void TriggerChannelEnded(Ability a, EnemyTarget? t, SimTime now, bool clipped) => OnChannelEnded?.Invoke(a, t, now, clipped);
    public void TriggerChannelTick(Ability a, EnemyTarget? t, double fraction, SimulationContext ctx) => OnChannelTick?.Invoke(a, t, fraction, ctx);

    // ------------------------------------------------------------------ resources and Spirit Refund

    /// <summary>Spirit Points toward the Spirit ability (every hero).</summary>
    public static readonly ResourceDef SpiritPoints = new("Spirit", Max: 100);

    /// <summary>The resources this hero has (declared with <see cref="AddResource"/> in <see cref="Initialize"/>).</summary>
    public List<ResourceDef> ResourceDefs { get; } = new();

    protected void AddResource(ResourceDef resource)
    {
        ResourceDefs.Add(resource);
        Resources.Register(resource, resource.Max, resource.Initial);
    }

    /// <summary>The resource named <paramref name="aplName"/> in action lists (its <see cref="ResourceDef.AplName"/> or an alias).</summary>
    public ResourceDef? FindResource(string aplName) =>
        aplName == SpiritPoints.AplName ? SpiritPoints
            : ResourceDefs.FirstOrDefault(r => r.AplName == aplName || r.Aliases.Contains(aplName));

    public double ResourceAmount(ResourceDef resource) => Resources.Get(resource).Current;

    /// <summary>
    /// Adds <paramref name="amount"/> up to the maximum; the part past the maximum overflows (<see cref="OnResourceOverflow"/>).
    /// Returns the amount added.
    /// </summary>
    public double GainResource(ResourceDef resource, double amount, SimTime now)
    {
        var state = Resources.Get(resource);
        double added = Math.Min(amount, Math.Max(0.0, state.Max - state.Current));
        state.Current += added;
        if (amount - added > 0) OnResourceOverflow?.Invoke(resource, amount - added, now);
        return added;
    }

    /// <summary>
    /// Spends <paramref name="amount"/>. When it is the hero's <see cref="SpiritRefundResource"/>, a Spirit Refund may give
    /// it back.
    /// </summary>
    public void SpendResource(ResourceDef resource, double amount, SimulationContext? ctx)
    {
        var state = Resources.Get(resource);
        state.Current = Math.Max(0.0, state.Current - amount);
        NotifyResourceSpent(resource, amount, ctx?.CurrentTime ?? default);
        if (amount <= 0 || ctx == null || resource != SpiritRefundResource) return;
        if (!RollSpiritRefund(ctx)) return;
        // Rising Spirit comes with the spend; the resource comes back a moment later.
        Auras.Apply(RisingSpirit, ctx.CurrentTime);
        ctx.Schedule(SimTime.FromSeconds(SpiritRefundDelaySeconds), c => GainResource(resource, amount, c.CurrentTime), "Spirit Refund");
    }

    /// <summary>Tells effects that count spent resource (e.g. cooldown reductions per point spent) that it was spent.</summary>
    public void NotifyResourceSpent(ResourceDef resource, double amount, SimTime now) => OnResourceSpent?.Invoke(resource, amount, now);

    /// <summary>The resource a Spirit Refund gives back: the hero's spender resource (e.g. Rime's Winter Orbs).</summary>
    public ResourceDef? SpiritRefundResource { get; protected set; }

    public const string SpiritRefundProc = "spirit_refund";
    /// <summary>Combat logs (10 Rime logs): refunded resource shows up again 0.233s (10th percentile) after the spend.</summary>
    public const double SpiritRefundDelaySeconds = 0.2;

    /// <summary>Rising Spirit (effect 3115): +5% primary stat per stack for 10s, up to 10 stacks, from Spirit Refunds.</summary>
    public const int RisingSpiritEffectId = 3115;
    public const double RisingSpiritSeconds = 10.0;
    public const int RisingSpiritMaxStacks = 10;
    public const double RisingSpiritPrimaryStatPerStack = 0.05;
    /// <summary>Combat logs (10 Rime logs): each further Spirit Refund adds 2s to the time left instead of restarting it.</summary>
    public const double RisingSpiritReapplySeconds = 2.0;

    public static readonly AuraDef RisingSpirit = AuraDef.Effect("Rising Spirit", RisingSpiritEffectId, RisingSpiritSeconds,
        RisingSpiritMaxStacks, StatModifier.Add(StatType.PrimaryAttributeMultiplier, RisingSpiritPrimaryStatPerStack))
        with { ReapplyExtendsSeconds = RisingSpiritReapplySeconds };

    /// <summary>Effects that make a Spirit Refund certain (e.g. Undulating Spirit): each returns true if it guaranteed this one.</summary>
    public List<Func<SimTime, bool>> GuaranteedSpiritRefunds { get; } = new();

    private bool RollSpiritRefund(SimulationContext ctx)
    {
        foreach (var guaranteed in GuaranteedSpiritRefunds)
        {
            if (guaranteed(ctx.CurrentTime)) return true;
        }
        // Spirit Refund chance is Spirit / (1 + Spirit) (per the user: "%/(100+%)").
        double spirit = SpiritPercent(ctx.CurrentTime);
        return ctx.Outcomes.RollProc(SpiritRefundProc, spirit / (1.0 + spirit), ctx, RisingSpirit.LogId);
    }

    // ------------------------------------------------------------------ auras

    public AuraDef Register(AuraDef def)
    {
        if (def.LogId > 0) ModeledAuraLogIds.Add(def.LogId);
        return def;
    }

    /// <summary>Activates Spirit of Heroism and the auras that last while it does.</summary>
    public void ActivateSpiritOfHeroism(SimulationContext ctx)
    {
        var heroism = SpiritOfHeroism.WithDuration(SpiritOfHeroismSeconds + SpiritOfHeroismDurationBonus);
        var applied = Auras.Apply(heroism, ctx.CurrentTime);
        foreach (var aura in SpiritOfHeroismAuras) Auras.Apply(aura, ctx.CurrentTime);
        // The companions end with Spirit of Heroism itself (on its actual expiry, which includes the server-frame lag).
        ctx.ScheduleAt(applied.ExpiresAt, c =>
        {
            if (Auras.HasAura(heroism, c.CurrentTime)) return;
            foreach (var aura in SpiritOfHeroismAuras) Auras.Remove(aura, c.CurrentTime);
        }, "Spirit of Heroism ends");
    }

    public double AbilityDamageMultiplier(Ability ability, SimTime now)
    {
        double mult = 1.0;
        foreach (var m in AbilityDamageMultipliers) mult *= m(ability, now);
        return mult;
    }

    /// <summary>
    /// How fast <paramref name="ability"/>'s cooldown ticks down (1 = real time). Recovery sources share one pool: each
    /// adds its bonus part (Chronoshift's 8x adds 7, Strand of Eternity's 1.1x adds 0.1, The Monarch its percentage).
    /// Charge abilities that recharge with haste add the haste on top. Weapon abilities ignore the pool except for their own
    /// effects: Chronoshift recovers at 8x while channeling it (per the user; 10 Rime logs: no early casts, and ruru's
    /// fastest comes back 1.1s before it is pressed, against 7.7s if weapons took the whole pool).
    /// (Fellowship Theorycrafting, "How Cooldowns Actually Work", measured from live logs.)
    /// </summary>
    public double CooldownRate(Ability ability, SimTime now)
    {
        bool weaponOwnOnly = ability.IgnoresCooldownRecovery;
        double pool = 1.0 + (weaponOwnOnly ? 0.0 : CooldownAcceleration(ability, now));
        foreach (var aura in Auras.Active(now))
        {
            // Weapon abilities only speed up from their own recovery effects (Chronoshift's barrier, per the user).
            if (weaponOwnOnly && !ability.OwnRecoveryAuraLogIds.Contains(aura.Def.LogId)) continue;
            foreach (var mod in aura.Modifiers)
            {
                if (mod.Type == StatType.CooldownRecovery) pool += mod.Value - 1.0;
            }
        }
        return pool + (ability.ChargesRechargeWithHaste ? GetHaste(now) : 0.0);
    }

    /// <summary>Recovery bonuses specific to an ability (e.g. The Monarch for Major abilities), as fractions.</summary>
    public double CooldownAcceleration(Ability ability, SimTime now)
    {
        double accel = 0.0;
        foreach (var a in CooldownAccelerations) accel += a(ability, now);
        return accel;
    }

    // ------------------------------------------------------------------ stats

    /// <summary>Primary attribute after all multipliers.</summary>
    public double PrimaryAttribute(SimTime now)
    {
        double value = Auras.Fold(StatType.PrimaryAttribute, Stats.PrimaryStat, now);
        return value * Auras.Fold(StatType.PrimaryAttributeMultiplier, 1.0, now);
    }

    public double CritChance(SimTime now) =>
        StatBlock.WithExtraRating(Stats.CritPercent, Stats.CritRating, Auras.Fold(StatType.CritRating, 0.0, now))
        + Auras.Fold(StatType.CritChance, 0.0, now);

    public double CritChanceAgainst(EnemyTarget? target, SimTime now)
    {
        double chance = CritChance(now);
        if (target != null)
        {
            foreach (var bonus in TargetCritChanceBonuses) chance += bonus(target, now);
        }
        return chance;
    }

    /// <summary>Damage multiplier of a critical hit.</summary>
    public double CritMultiplier(SimTime now) => Auras.Fold(StatType.CritMultiplier, StatBlock.BaseCritMultiplier, now);

    /// <summary>Total haste at <paramref name="now"/>: rating haste (with rating buffs) plus haste from auras.</summary>
    public double GetHaste(SimTime now) => Math.Max(0.0,
        StatBlock.WithExtraRating(Stats.HastePercent, Stats.HasteRating, Auras.Fold(StatType.HasteRating, 0.0, now))
        + Auras.Fold(StatType.Haste, 0.0, now));

    public double Expertise(SimTime now) =>
        StatBlock.WithExtraRating(Stats.ExpertisePercent, Stats.ExpertiseRating, Auras.Fold(StatType.ExpertiseRating, 0.0, now))
        + Auras.Fold(StatType.Expertise, 0.0, now);

    /// <summary>Spirit as a fraction (0.30 = 30% Spirit).</summary>
    public double SpiritPercent(SimTime now) =>
        StatBlock.WithExtraRating(Stats.SpiritPercent, Stats.SpiritRating, Auras.Fold(StatType.SpiritRating, 0.0, now))
        + Auras.Fold(StatType.Spirit, 0.0, now);

    /// <summary>Outgoing damage multiplier from auras ("DamageScale").</summary>
    public double DamageScale(SimTime now) => Auras.Fold(StatType.DamageScale, 1.0, now);

    public double DamageMultiplierAgainst(EnemyTarget? target, SimTime now)
    {
        double mult = 1.0;
        if (target != null)
        {
            foreach (var m in TargetDamageMultipliers) mult *= m(target, now);
        }
        return mult;
    }

    // ------------------------------------------------------------------ cooldowns and charges

    public CooldownState CooldownOf(Ability ability)
    {
        if (!Cooldowns.TryGetValue(ability.Name, out var state))
        {
            state = new CooldownState(ability.MaxCharges,
                rateAt: now => CooldownRate(ability, now),
                nextCheckpoint: now => Auras.NextExpiry(now));
            Cooldowns[ability.Name] = state;
        }
        return state;
    }

    public bool IsCooldownReady(string name, SimTime now) =>
        !Cooldowns.TryGetValue(name, out var state) || state.ChargesAt(now) > 0;

    public double GetCooldownRemains(string name, SimTime now) =>
        Cooldowns.TryGetValue(name, out var state) && state.ChargesAt(now) == 0
            ? Math.Max(0.0, (state.NextChargeAt - now).TotalSeconds)
            : 0.0;

    public void ReduceCooldown(string name, SimTime amount, SimTime now = default)
    {
        if (Cooldowns.TryGetValue(name, out var state)) state.Reduce(amount, now);
    }

    public void ReduceCooldown<T>(SimTime amount, SimTime now = default) where T : Ability
    {
        var ability = GetAbility<T>();
        if (ability != null) ReduceCooldown(ability.Name, amount, now);
    }

    public bool IsCooldownReady<T>(SimTime now) where T : Ability
    {
        var ability = GetAbility<T>();
        return ability == null || IsCooldownReady(ability.Name, now);
    }

    public double GetCooldownRemains<T>(SimTime now) where T : Ability
    {
        var ability = GetAbility<T>();
        return ability != null ? GetCooldownRemains(ability.Name, now) : 0.0;
    }

    public abstract void Initialize();
    public abstract void ResetForIteration();

    public Ability? GetAbility(string name) =>
        Abilities.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public T? GetAbility<T>() where T : Ability =>
        Abilities.OfType<T>().FirstOrDefault();

    public bool HasTalent<T>() where T : Talent => Talents.OfType<T>().Any();

    public T? GetTalent<T>() where T : Talent =>
        Talents.OfType<T>().FirstOrDefault();


    protected void ResetCombatState()
    {
        Resources.Reset();
        Auras.ClearAll();
        Cooldowns.Clear();
        GlobalCooldownEndTime = SimTime.Zero;
        CurrentCast = null;
        CurrentChannel = null;
        foreach (var passive in PassiveAuras) Auras.Apply(passive, SimTime.Zero);
    }
}

/// <summary>
/// Charges and recharge of one ability. A spent charge starts recharging immediately if none is recharging;
/// charges recharge one at a time. The timer holds cooldown-seconds and drains at a rate (1 = real time) that may
/// change while recharging (cooldown recovery, haste for charge abilities); the rate is re-read whenever
/// <see cref="Advance"/> is called and at the checkpoints given (e.g. aura expiries).
/// </summary>
public sealed class CooldownState
{
    private readonly Func<SimTime, double> _rateAt;
    private readonly Func<SimTime, SimTime> _nextCheckpoint;
    private int _charges;
    private double _remaining;   // cooldown-seconds left on the charge recharging
    private double _rechargeBase;
    private SimTime _last;
    private double _rate = 1.0;
    private bool _advancing;

    public CooldownState(int maxCharges, Func<SimTime, double>? rateAt = null, Func<SimTime, SimTime>? nextCheckpoint = null)
    {
        MaxCharges = Math.Max(1, maxCharges);
        _charges = MaxCharges;
        _rateAt = rateAt ?? (_ => 1.0);
        _nextCheckpoint = nextCheckpoint ?? (_ => SimTime.Infinite);
    }

    public int MaxCharges { get; }
    private bool Recharging => _charges < MaxCharges;

    /// <summary>When the charge recharging now returns at the current rate (meaningful only while recharging).</summary>
    public SimTime NextChargeAt => _last + SimTime.FromSeconds(_remaining / Math.Max(1e-9, _rate));

    public int ChargesAt(SimTime now)
    {
        Advance(now);
        return _charges;
    }

    /// <summary>Spends a charge; a recharge of <paramref name="rechargeSeconds"/> cooldown-seconds starts if none is running.</summary>
    public void Spend(SimTime now, double rechargeSeconds)
    {
        Advance(now);
        if (_charges <= 0) return;
        bool wasFull = _charges == MaxCharges;
        _charges--;
        if (wasFull)
        {
            _rechargeBase = rechargeSeconds;
            _remaining = rechargeSeconds;
        }
    }

    /// <summary>
    /// Takes <paramref name="amount"/> cooldown-seconds off the charge recharging (a flat shave: worth fewer real seconds
    /// while recovery is fast).
    /// </summary>
    public void Reduce(SimTime amount, SimTime now)
    {
        Advance(now);
        if (!Recharging) return;
        _remaining -= amount.TotalSeconds;
        while (Recharging && _remaining <= 0)
        {
            _charges++;
            if (Recharging) _remaining += _rechargeBase;
        }
        if (!Recharging) _remaining = 0;
    }

    public void ResetAll()
    {
        _charges = MaxCharges;
        _remaining = 0;
    }

    /// <summary>Gives back one charge now (a forced cast in a replay); the recharge in progress carries on.</summary>
    public void GrantCharge(SimTime now)
    {
        Advance(now);
        if (_charges >= MaxCharges) return;
        _charges++;
        if (!Recharging) _remaining = 0;
    }

    /// <summary>Progresses the recharge to <paramref name="now"/>, re-reading the rate at every checkpoint on the way.</summary>
    public void Advance(SimTime now)
    {
        if (now == default || now < _last || _advancing) return;
        _advancing = true;
        try
        {
            var t = _last;
            while (t < now)
            {
                if (!Recharging) { t = now; break; }
                var next = _nextCheckpoint(t);
                if (next <= t || next > now) next = now;
                double span = (next - t).TotalSeconds;
                double rate = Math.Max(1e-9, _rate);
                if (_remaining <= span * rate)
                {
                    // A charge returns before the next checkpoint.
                    t += SimTime.FromSeconds(_remaining / rate);
                    _charges++;
                    _remaining = Recharging ? _rechargeBase : 0;
                    continue;
                }
                _remaining -= span * rate;
                t = next;
                _rate = _rateAt(t);
            }
            _last = now;
            _rate = _rateAt(now);
        }
        finally { _advancing = false; }
    }
}
