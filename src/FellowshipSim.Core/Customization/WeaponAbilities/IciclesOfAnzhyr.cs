namespace FellowshipSim.Core.Customization.WeaponAbilities;

/// <summary>
/// Weapon ability 1561 (Frozen Staff of An'zhyr, weapon power 357): "Conjure 3 waves of icicles to pelt the target area,
/// dealing 144% Main Stat frost damage to all enemies in the area with each wave. Enemies hit by the final wave are
/// afflicted by Curse of An'zhyr permanently, taking 38.4% Main Stat frost damage every 3 seconds. Enemies afflicted by
/// Curse of An'zhyr take +200% increased direct damage from Icicles of An'zhyr. Can be cast during Global Cooldown."
/// Combat logs (PCaMVTFbZzNLrfx3 fight 5, 12 casts): the waves land 0.27s, 1.27s and 2.27s after the cast and keep the
/// damage, crit chance and crit power of the cast; damage falls off as sqrt(15/N) beyond 15 enemies (every wave on 16-22
/// enemies fits T = 15.0-15.6; all 12 casts then match within 0.97-1.06). The curse (effect 2286) ticks every 3s
/// shortened by haste.
/// </summary>
public class IciclesOfAnzhyr : Ability
{
    public const int AbilityId = 1561;
    public const string AbilityName = "Icicles of An'zhyr";
    public const double Coefficient = 1.44;
    public const double CooldownSeconds = 40.0;
    public const int Waves = 3;
    public const double FirstWaveSeconds = 0.27;
    public const double WaveIntervalSeconds = 1.0;
    public const double TargetThreshold = 15;
    public const double CursedDamageMultiplier = 3.0;

    public const int CurseEffectId = 2286;
    public const string CurseName = "Curse of An'zhyr";
    public const double CurseCoefficient = 0.384;
    public const double CurseTickSeconds = 3.0;

    /// <summary>The curse on an enemy (permanent).</summary>
    public static readonly AuraDef Curse = AuraDef.Effect(CurseName, CurseEffectId);

    private readonly CurseTick _curse;

    public IciclesOfAnzhyr(Hero owner) : base(owner, AbilityId, AbilityName, AbilityCategory.Weapon)
    {
        BaseCoefficient = Coefficient;
        AoeTargetThreshold = TargetThreshold;
        SetCooldown(CooldownSeconds);
        IgnoresGcd = true;
        _curse = new CurseTick(owner);
    }

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        TriggerCooldown(ctx);
        // The waves keep the damage, crit chance and crit power of the cast.
        var now = ctx.CurrentTime;
        double damage = BaseDamage(ctx, null);
        double critChance = Owner.CritChance(now) + CritChanceBonus;
        double critMultiplier = CritMultiplierAt(now);
        for (int i = 0; i < Waves; i++)
        {
            bool final = i == Waves - 1;
            ctx.Schedule(SimTime.FromSeconds(FirstWaveSeconds + i * WaveIntervalSeconds),
                c => Wave(c, target, final, damage, critChance, critMultiplier), $"{Name} wave {i + 1}");
        }
    }

    private void Wave(SimulationContext ctx, EnemyTarget target, bool final, double damage, double critChance, double critMultiplier)
    {
        var enemies = ctx.ActiveEnemies.ToList();
        if (enemies.Count == 0 && target is { IsSleeping: false }) enemies.Add(target);
        var struck = ctx.Outcomes.SelectTargets(DamageLogId, enemies, ctx);
        double falloff = struck.Count > TargetThreshold ? Math.Sqrt(TargetThreshold / struck.Count) : 1.0;
        foreach (var enemy in struck)
        {
            bool cursed = enemy.Auras.HasAura(Curse, ctx.CurrentTime);
            double multiplier = falloff * (cursed ? CursedDamageMultiplier : 1.0);
            ctx.ApplyDamage(Owner, enemy, DamageLogId, Name, canCrit: true,
                c => new SimulationContext.HitNumbers(
                    damage * multiplier * Owner.DamageMultiplierAgainst(enemy, c.CurrentTime),
                    critChance + Owner.CritChanceAgainst(enemy, c.CurrentTime) - Owner.CritChance(c.CurrentTime),
                    critMultiplier),
                snapshot: true);
            if (final && !cursed) Afflict(ctx, enemy);
        }
    }

    /// <summary>
    /// Curse of An'zhyr: permanent, ticking every 3s (hasted) while the enemy lives, with the stats at each tick.
    /// UNVERIFIED: PCaMVTFbZzNLrfx3 fight 5 ticks are 13% below the engine this way, and 20% below if the curse snapshots
    /// the stats it was applied with (Icicles is usually cast during burst buffs).
    /// </summary>
    private void Afflict(SimulationContext ctx, EnemyTarget enemy)
    {
        enemy.Auras.Apply(Curse, ctx.CurrentTime);
        void Tick(SimulationContext c)
        {
            if (enemy.IsSleeping) return;
            _curse.DealDamage(c, enemy);
            HastedTimer.Start(c, Owner, CurseTickSeconds, Tick, CurseName);
        }
        HastedTimer.Start(ctx, Owner, CurseTickSeconds, Tick, CurseName);
    }

    /// <summary>The curse's damage numbers (logged as effect 2286).</summary>
    private sealed class CurseTick : Ability
    {
        public CurseTick(Hero owner) : base(owner, LogIds.Effect(CurseEffectId), CurseName, AbilityCategory.Weapon)
        {
            BaseCoefficient = CurseCoefficient;
        }

        public override void Execute(SimulationContext ctx, EnemyTarget target) { }
    }
}
