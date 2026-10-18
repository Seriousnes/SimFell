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

    /// <summary>Rime's talent tree, as in game (players write picks as row + column: "1b + 2a + 2c ...").</summary>
    public static readonly TalentSlot[] TalentTree = TalentSlot.Tree(
        //              row cost  a                            b                              c
        TalentSlot.InRow(1, 2,    Avalanche.TalentId,          CoalescingFrost.TalentId,      GlacialAssault.TalentId),
        TalentSlot.InRow(2, 1,    ColdShower.TalentId,         ChillingFinesse.TalentId,      Burstbolter.TalentId),
        TalentSlot.InRow(3, 2,    CascadingBlitz.TalentId,     HarrowingIce.TalentId,         IcyFlow.TalentId),
        TalentSlot.InRow(4, 1,    BurstingSwallows.TalentId,   GreaterGlacialBlast.TalentId,  NavirsKeeper.TalentId),
        TalentSlot.InRow(5, 3,    IcyTalons.TalentId,          FrostweaversWrath.TalentId,    SoulfrostTorrent.TalentId),
        TalentSlot.InRow(6, 1,    BitingCold.TalentId,         SupremeTorrent.TalentId,       WisdomOfTheNorth.TalentId));

    /// <summary>Anima was generated (before it turns into Winter Orbs); the target is the one the Anima came from.</summary>
    public event Action<SimulationContext, double, EnemyTarget>? AnimaGenerated;

    /// <summary>Winter Orbs were gained (raised once per gain that added orbs, with how many).</summary>
    public event Action<SimulationContext, int>? WinterOrbsGained;

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
    }

    /// <summary>
    /// Buffs a Winter Orb spender (Glacial Blast, Ice Comet, Talon Strike, Rising Talons, free comets) consumes for crit
    /// chance on its hits. Each talent adds its own (Frostweaver's Wrath, Icy Flow): it consumes its buff and returns the
    /// crit chance it gives. The flag is true for Avalanche's extra comets.
    /// </summary>
    public List<Func<SimulationContext, bool, double>> SpenderCritBuffs { get; } = new();

    /// <summary>Consumes the spender crit buffs for one cast; returns the crit chance its hits get.</summary>
    public double ConsumeSpenderCritBuffs(SimulationContext ctx, bool avalancheComet = false) =>
        SpenderCritBuffs.Sum(buff => buff(ctx, avalancheComet));

    /// <summary>Tells talents a Frost Swallow hit (Bursting Swallows).</summary>
    public void OnFrostSwallowHit(SimulationContext ctx, EnemyTarget target) => FrostSwallowHit?.Invoke(ctx, target);

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
        GetAbility<FlightOfTheNavir>()?.ResetState();
    }
}
