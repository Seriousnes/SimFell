using FellowshipSim.Core.Customization.Legendaries;
using FellowshipSim.Core.Customization.WeaponAbilities;
using FellowshipSim.Core.Heroes.Rime.Abilities;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime;

/// <summary>
/// Rime, the Harbinger of Winter. Her abilities build Anima; every 9 Anima become a Winter Orb, which her spenders use.
/// Abilities live in Abilities/, talents in Talents/ (each talent hooks the events below or the abilities' own events).
/// </summary>
public sealed class Rime : Hero
{
    public const int MaxWinterOrbs = 5;
    public const double AnimaPerWinterOrb = 9.0;
    /// <summary>
    /// Undocumented in the game (game data ability 1033 lists "Rime.AutoDamageProjectile.OrbReward" as its source):
    /// every Winter Orb earned sends 3 Frost Swallows at enemies.
    /// </summary>
    public const int SwallowsPerWinterOrb = 3;

    /// <summary>Anima: combat logs report it as resource type 2, in hundredths.</summary>
    public static readonly ResourceDef Anima = new("Anima", Max: 100, LogResourceType: 2, LogScale: 0.01, LogTolerance: 0.99);

    /// <summary>Winter Orbs: combat logs report them as resource type 5, in hundredths.</summary>
    public static readonly ResourceDef WinterOrbsResource = new("Winter Orbs", Max: MaxWinterOrbs, LogResourceType: 5, LogScale: 0.01)
    {
        Aliases = ["worb"],
    };

    /// <summary>
    /// Rime's class tuning aura, with the date each value took effect: hero attribute BalancingDamageAura = 1.04 (+4%
    /// damage) since 2026-08-27 (per the user; logs before then run ~4% below the engine with it). Source:
    /// fs_tc_uploads s3/hero_data.json (Rime.Attributes.BalancingDamageAura).
    /// </summary>
    public static readonly (DateTime From, double DamageMultiplier)[] ClassDamageAuraHistory =
    [
        (new DateTime(2026, 8, 27, 0, 0, 0, DateTimeKind.Utc), 1.04),
    ];

    /// <summary>Every aura Rime's abilities, talents and legendary can put on her (the replay compares them with the log).</summary>
    public static readonly AuraDef[] AllAuras =
    [
        GlacialAssault.Buff, NavirsKeeper.Buff, FlightOfTheNavir.Buff, WintersBlessing.Buff, BurstingIce.WintersEmbrace,
        BurstingIce.Debuff, IceBlitz.Buff, WrathOfWinter.Buff, UndulatingSpirit.Buff, RisingSpirit, Chronoshift.Barrier,
        HarrowingIce.Buff, IcyFlow.Buff, FrostweaversWrath.Buff, SoulfrostTorrent.Buff,
    ];

    /// <summary>Anima was generated (before it turns into Winter Orbs); the target is the one the Anima came from.</summary>
    public event Action<SimulationContext, double, EnemyTarget>? AnimaGenerated;

    /// <summary>Winter Orbs were gained (raised once per gain that added orbs, with how many).</summary>
    public event Action<SimulationContext, int>? WinterOrbsGained;

    /// <summary>An Ice Comet was cast at a target (Ice Comet, Rising Talons with Icy Talons, or a free Cold Shower comet).</summary>
    public event Action<SimulationContext, EnemyTarget>? IceCometCast;

    /// <summary>A Frost Swallow hit an enemy.</summary>
    public event Action<SimulationContext, EnemyTarget>? FrostSwallowHit;

    /// <summary>Icy Talons: Glacial Blast and Ice Comet are replaced by Talon Strike and Rising Talons.</summary>
    public bool HasIcyTalons { get; internal set; }

    public int WinterOrbs
    {
        get => (int)ResourceAmount(WinterOrbsResource);
        set => Resources.Get(WinterOrbsResource).Current = value;
    }

    public int GlacialAssaultStacks => Auras.GetStacks(GlacialAssault.Buff);

    public override void Initialize()
    {
        AddResource(Anima);
        AddResource(WinterOrbsResource);
        SpiritRefundResource = WinterOrbsResource;
        foreach (var aura in AllAuras) Register(aura);
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
        if (Auras.HasAura(FrostweaversWrath.Buff, now))
        {
            Auras.ConsumeStack(FrostweaversWrath.Buff, now);
            crit += FrostweaversWrath.CritChance;
        }
        if (consumesIcyFlow && Auras.HasAura(IcyFlow.Buff, now))
        {
            Auras.ConsumeStack(IcyFlow.Buff, now);
            crit += IcyFlow.CritChance;
        }
        return crit;
    }

    /// <summary>Tells talents an Ice Comet was cast (Avalanche).</summary>
    public void OnIceCometCast(SimulationContext ctx, EnemyTarget target)
    {
        if (target != null) IceCometCast?.Invoke(ctx, target);
    }

    /// <summary>Tells talents a Frost Swallow hit (Bursting Swallows).</summary>
    public void OnFrostSwallowHit(SimulationContext ctx, EnemyTarget target) => FrostSwallowHit?.Invoke(ctx, target);

    /// <summary>A free Ice Comet on <paramref name="target"/> (Cold Shower): it consumes spender buffs and can trigger Avalanche.</summary>
    public void CastFreeIceComet(SimulationContext ctx, EnemyTarget target)
    {
        var comet = GetAbility<ProcIceComet>();
        if (comet == null || target == null) return;
        comet.Strike(ctx, target, ConsumeSpenderCritBuffs(ctx));
        OnIceCometCast(ctx, target);
    }

    /// <summary>Spends Winter Orbs (a Spirit Refund may give them back).</summary>
    public void SpendWinterOrbs(int count, SimulationContext? ctx = null) => SpendResource(WinterOrbsResource, count, ctx);

    /// <summary>Gains Winter Orbs one at a time; each earned orb also sends Frost Swallows at enemies.</summary>
    public void GenerateWinterOrbs(int count, SimulationContext? ctx = null, EnemyTarget? target = null)
    {
        int before = WinterOrbs;
        for (int i = 0; i < count; i++)
        {
            if (ctx != null) GetAbility<FrostSwallows>()?.AttackEnemies(ctx, SwallowsPerWinterOrb);
            GainResource(WinterOrbsResource, 1, ctx?.CurrentTime ?? default);
        }
        if (ctx != null && WinterOrbs > before) WinterOrbsGained?.Invoke(ctx, WinterOrbs - before);
    }

    /// <summary>Flight of the Navir: Cold Snap and Freezing Torrent command the 5 circling swallows to attack.</summary>
    public void TriggerFrostSwallows(SimulationContext ctx, EnemyTarget target)
    {
        if (!Auras.HasAura(FlightOfTheNavir.Buff, ctx.CurrentTime) || target == null) return;
        var swallows = GetAbility<CommandedFrostSwallows>();
        if (swallows == null) return;
        // Every swallow circling now; one that lands at once must not be sent again by the same command.
        int available = swallows.Available;
        for (int i = 0; i < available; i++) swallows.Send(ctx, target);
    }

    /// <summary>Adds Anima; every 9 Anima become a Winter Orb.</summary>
    public void AddAnima(double amount, SimulationContext? ctx = null, EnemyTarget? target = null)
    {
        GainResource(Anima, amount, ctx?.CurrentTime ?? default);

        var primaryTarget = target ?? CurrentTarget ?? ctx?.ActiveEnemies.FirstOrDefault();
        if (ctx != null && primaryTarget != null) AnimaGenerated?.Invoke(ctx, amount, primaryTarget);

        while (ResourceAmount(Anima) >= AnimaPerWinterOrb)
        {
            Resources.Consume(Anima, AnimaPerWinterOrb);
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
