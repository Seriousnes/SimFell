using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime;

public sealed class Rime : Hero
{
    public const int MaxWinterOrbs = 5;
    public const double AnimaPerWinterOrb = 9.0;
    /// <summary>
    /// Undocumented in the game (datamine ability 1033 lists "Rime.AutoDamageProjectile.OrbReward" as its source):
    /// every Winter Orb earned sends 3 Frost Swallows at enemies.
    /// </summary>
    public const int SwallowsPerWinterOrb = 3;
    /// <summary>Datamine ability 1016 "Rime.SpiritProc.ResourceRefund" (Spirit Refund).</summary>
    public const string SpiritRefundProc = "spirit_refund";
    public const double RefundDelaySeconds = 0.2;

    /// <summary>
    /// Rime's class tuning aura, with the date each value took effect: hero attribute BalancingDamageAura = 1.04 (+4%
    /// damage) since 2026-08-27 (per the user; logs before then run ~4% below the engine with it). Hero attributes are not
    /// in the Codex SDK export; source: fs_tc_uploads s3/hero_data.json (Rime.Attributes.BalancingDamageAura).
    /// </summary>
    public static readonly (DateTime From, double DamageMultiplier)[] ClassDamageAuraHistory =
    [
        (new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Utc), 1.04),
    ];

    public Rime(GameData? data = null) : base(data) { }

    public RimeAuras Aura { get; private set; } = null!;

    public int WinterOrbs
    {
        get => (int)Resources.Get(ResourceType.WinterOrbs).Current;
        set => Resources.Get(ResourceType.WinterOrbs).Current = value;
    }

    public int GlacialAssaultStacks => Auras.GetStacks(Aura.GlacialAssault);

    public override void Initialize()
    {
        Resources.Register(ResourceType.Anima, max: 100, initial: 0);
        Resources.Register(ResourceType.WinterOrbs, max: MaxWinterOrbs, initial: 0);
        Aura = new RimeAuras(this);
        AddClassTuningAura("Rime class tuning", ClassDamageAuraHistory);

        Abilities.Add(new ColdSnap(this));
        Abilities.Add(new GlacialBlast(this));
        Abilities.Add(new TalonStrike(this));
        Abilities.Add(new RisingTalons(this));
        Abilities.Add(new BurstingIce(this));
        Abilities.Add(new FreezingTorrent(this));
        Abilities.Add(new FrostBolt(this));
        Abilities.Add(new IceComet(this));
        Abilities.Add(new IceBlitz(this));
        Abilities.Add(new WintersBlessing(this));
        Abilities.Add(new FlightOfTheNavir(this));
        Abilities.Add(new WrathOfWinter(this));
        Abilities.Add(new FrostSwallows(this));
        Abilities.Add(new CommandedFrostSwallows(this));
        Abilities.Add(new ProcIceComet(this));
        Abilities.Add(new CoalescingFrostEruption(this));
    }

    /// <summary>
    /// The crit chance a Winter Orb spender cast (Glacial Blast, Ice Comet, Talon Strike, Rising Talons) gets from the buffs
    /// it consumes: a Frostweaver's Wrath stack, and an Icy Flow stack when <paramref name="consumesIcyFlow"/>.
    /// </summary>
    public double ConsumeSpenderCritBuffs(SimulationContext ctx, bool consumesIcyFlow = true)
    {
        var now = ctx.CurrentTime;
        double crit = 0.0;
        if (Auras.HasAura(Aura.FrostweaversWrath, now))
        {
            Auras.ConsumeStack(Aura.FrostweaversWrath, now);
            crit += FrostweaversWrath.CritChance;
        }
        if (consumesIcyFlow && Auras.HasAura(Aura.IcyFlow, now))
        {
            Auras.ConsumeStack(Aura.IcyFlow, now);
            crit += IcyFlow.CritChance;
        }
        return crit;
    }

    /// <summary>Avalanche on an Ice Comet (or Rising Talons) cast: 18% for 1 extra comet, else 9% for 2.</summary>
    public void TriggerAvalanche(SimulationContext ctx, EnemyTarget target)
    {
        if (!HasTalent<Avalanche>() || target == null) return;
        int extra = ctx.Outcomes.RollProc(Avalanche.ProcKey, Avalanche.OneExtraChance, ctx) ? 1
            : ctx.Outcomes.RollProc(Avalanche.ProcKey, Avalanche.TwoExtraChance, ctx) ? 2 : 0;
        var comet = GetAbility<ProcIceComet>();
        for (int i = 1; comet != null && i <= extra; i++)
        {
            ctx.Schedule(SimTime.FromSeconds(Avalanche.CometSpacingSeconds * i), c =>
            {
                // The extra comets consume Frostweaver's Wrath ("the next ... Ice Comet that deals damage") but not Icy Flow.
                comet.Strike(c, target, ConsumeSpenderCritBuffs(c, consumesIcyFlow: false));
            }, "Avalanche comet");
        }
    }

    /// <summary>Cold Shower: a free Ice Comet on <paramref name="target"/> (it consumes spender buffs and can trigger Avalanche).</summary>
    public void CastFreeIceComet(SimulationContext ctx, EnemyTarget target)
    {
        var comet = GetAbility<ProcIceComet>();
        if (comet == null || target == null) return;
        comet.Strike(ctx, target, ConsumeSpenderCritBuffs(ctx));
        TriggerAvalanche(ctx, target);
    }

    /// <summary>
    /// Spends Winter Orbs. A Spirit Refund (chance equal to Spirit, or guaranteed by an Undulating Spirit stack)
    /// gives them back and grants Rising Spirit.
    /// </summary>
    public void SpendWinterOrbs(int count, SimulationContext? ctx = null)
    {
        WinterOrbs = Math.Max(0, WinterOrbs - count);
        if (HasTalent<WisdomOfTheNorth>()) WisdomOfTheNorth.ReduceCooldowns(this, count, ctx?.CurrentTime ?? default);
        if (count <= 0 || ctx == null) return;

        bool refund;
        if (Auras.HasAura(Aura.UndulatingSpirit, ctx.CurrentTime))
        {
            Auras.ConsumeStack(Aura.UndulatingSpirit, ctx.CurrentTime);
            refund = true;
        }
        else
        {
            // Spirit Refund chance is Spirit / (1 + Spirit) (per the user: "%/(100+%)").
            double spirit = SpiritPercent(ctx.CurrentTime);
            refund = ctx.Outcomes.RollProc(SpiritRefundProc, spirit / (1.0 + spirit), ctx, Aura.RisingSpirit.LogId);
        }

        if (refund)
        {
            // Rising Spirit comes with the spend; the orbs come back a moment later
            // (combat logs, 10 Rime logs: orbs show up again 0.233s (10th percentile) after, with snapshot lag; 0.2s).
            Auras.Apply(Aura.RisingSpirit, ctx.CurrentTime);
            ctx.Schedule(SimTime.FromSeconds(RefundDelaySeconds), c =>
            {
                // Refunded orbs past the maximum overflow (Wisdom of the North counts orbs that overflow your maximum).
                int overflow = Math.Max(0, WinterOrbs + count - MaxWinterOrbs);
                WinterOrbs = Math.Min(MaxWinterOrbs, WinterOrbs + count);
                if (overflow > 0 && HasTalent<WisdomOfTheNorth>()) WisdomOfTheNorth.ReduceCooldowns(this, overflow, c.CurrentTime);
            }, "Spirit Refund");
        }
    }

    public void GenerateWinterOrbs(int count, SimulationContext? ctx = null, EnemyTarget? target = null)
    {
        int before = WinterOrbs;
        for (int i = 0; i < count; i++)
        {
            if (ctx != null) GetAbility<FrostSwallows>()?.AttackEnemies(ctx, SwallowsPerWinterOrb);
            if (WinterOrbs < MaxWinterOrbs)
            {
                WinterOrbs++;
            }
            else if (HasTalent<WisdomOfTheNorth>())
            {
                // Datamine talent 49: "...for each Winter Orb you spend or that overflows your maximum."
                WisdomOfTheNorth.ReduceCooldowns(this, 1, ctx?.CurrentTime ?? default);
            }
        }
        // Frostweaver's Wrath: "Each time you generate Winter Orbs" (one roll per gain that adds orbs).
        if (ctx != null && WinterOrbs > before && HasTalent<FrostweaversWrath>()
            && ctx.Outcomes.RollProc(FrostweaversWrath.ProcKey, FrostweaversWrath.ProcChance, ctx, Aura.FrostweaversWrath.LogId))
        {
            Auras.Apply(Aura.FrostweaversWrath, ctx.CurrentTime);
        }
    }

    /// <summary>Flight of the Navir: Cold Snap and Freezing Torrent command the 5 circling swallows to attack.</summary>
    public void TriggerFrostSwallows(SimulationContext ctx, EnemyTarget target)
    {
        if (!Auras.HasAura(Aura.FlightOfTheNavir, ctx.CurrentTime) || target == null) return;
        var swallows = GetAbility<CommandedFrostSwallows>();
        if (swallows == null) return;
        // Every swallow circling now; one that lands at once must not be sent again by the same command.
        int available = swallows.Available;
        for (int i = 0; i < available; i++) swallows.Send(ctx, target);
    }

    public void AddAnima(double amount, SimulationContext? ctx = null, EnemyTarget? target = null)
    {
        Resources.Add(ResourceType.Anima, amount);

        var primaryTarget = target ?? CurrentTarget ?? ctx?.ActiveEnemies.FirstOrDefault();

        // Datamine talent 192 Cascading Blitz: "For each Anima you generate while your Ice Blitz is active,
        // you conjure 1 Frost Swallow to attack your target."
        if (ctx != null && primaryTarget != null && HasTalent<CascadingBlitz>() && Auras.HasAura(Aura.IceBlitz, ctx.CurrentTime))
        {
            // "...attack your target" (a replay follows the log if the player briefly had another target).
            var swallows = GetAbility<FrostSwallows>();
            for (int i = 0; swallows != null && i < (int)amount; i++)
            {
                var enemy = ctx.Outcomes.ChooseTarget(swallows.DamageLogId, ctx.ActiveEnemies.ToList(), ctx, preferred: primaryTarget);
                if (enemy != null) swallows.Attack(ctx, enemy, 1);
            }
        }

        while (Resources.Get(ResourceType.Anima).Current >= AnimaPerWinterOrb)
        {
            Resources.Consume(ResourceType.Anima, AnimaPerWinterOrb);
            GenerateWinterOrbs(1, ctx, primaryTarget);
        }
    }

    public override void ResetForIteration()
    {
        ResetCombatState();
        GetAbility<BurstingIce>()?.ResetState();
        GetAbility<CommandedFrostSwallows>()?.ResetState();
        GetAbility<CoalescingFrostEruption>()?.ResetState();
    }
}
