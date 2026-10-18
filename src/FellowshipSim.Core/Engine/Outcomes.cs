namespace FellowshipSim.Core.Engine;

/// <summary>Everything the damage pipeline knows about a hit before its random parts are decided.</summary>
public readonly record struct HitRequest(
    Hero Source,
    EnemyTarget? Target,
    int AbilityId,
    string AbilityName,
    double BaseDamage,
    bool CanCrit,
    double CritChance,
    double CritMultiplier,
    SimTime Time,
    double TickFraction = 1.0);

/// <summary>
/// What is decided when a hit is released: whether it crits, how long it travels before landing, and a damage
/// spread roll. Damage itself is computed when the hit lands, from the stats at that moment.
/// </summary>
/// <param name="Missed">The hit never lands (e.g. its target died while it travelled): no damage, nothing triggers.</param>
public readonly record struct HitOutcome(bool IsCrit, SimTime LandingDelay = default, double Spread = 1.0, object? Token = null, bool Missed = false);

/// <summary>
/// Decides every random outcome in the simulation: proc rolls, crits and damage spread.
/// Game code asks this instead of rolling dice itself, so the same mechanics can run against
/// random rolls, expected values, or outcomes supplied from elsewhere.
/// </summary>
public interface IOutcomeSource
{
    /// <summary>
    /// Rolls a named proc. <paramref name="procKey"/> identifies the proc, e.g. "inspired_allegiance";
    /// <paramref name="evidenceLogId"/> is the log ID of what a success produces (the aura it applies or the hit it deals),
    /// and <paramref name="effectDelaySeconds"/> how long after the roll that may appear (e.g. a projectile's travel).
    /// </summary>
    /// <remarks><paramref name="evidenceTarget"/>: the proc's effect lands on this enemy (a replay then only accepts evidence there).</remarks>
    bool RollProc(string procKey, double chance, SimulationContext ctx, int evidenceLogId = 0, double effectDelaySeconds = 0.0, EnemyTarget? evidenceTarget = null);

    /// <summary>
    /// Which of <paramref name="candidates"/> a multi-target hit with damage ID <paramref name="damageLogId"/> strikes
    /// (enemies in its area). Simulations hit every candidate; a replay answers from the log.
    /// </summary>
    IReadOnlyList<EnemyTarget> SelectTargets(int damageLogId, IReadOnlyList<EnemyTarget> candidates, SimulationContext ctx);

    /// <summary>
    /// The enemy a hit that picks its own target (e.g. a summoned attacker) strikes, out of <paramref name="candidates"/>.
    /// Simulations pick at random; a replay answers from the log.
    /// </summary>
    /// <remarks>
    /// With <paramref name="preferred"/> the hit normally goes there (e.g. "attacks your target"); a replay still
    /// follows the log when the log shows it elsewhere.
    /// </remarks>
    EnemyTarget? ChooseTarget(int damageLogId, IReadOnlyList<EnemyTarget> candidates, SimulationContext ctx, EnemyTarget? preferred = null);

    /// <summary>Decides a hit's crit, travel time and spread when it is released.</summary>
    HitOutcome ResolveHit(in HitRequest hit, SimulationContext ctx);

    /// <summary>
    /// The hit lands: returns the multiplier on <paramref name="landed"/>'s base damage (crit and spread), where
    /// <paramref name="landed"/> carries the base damage and crit values computed at landing.
    /// </summary>
    double Land(in HitRequest landed, HitOutcome outcome, SimulationContext ctx);

    /// <summary>Called once a landed hit has been dealt and everything it triggered (procs, follow-up hits) has run.</summary>
    void HitFinished(SimulationContext ctx);
}

public enum OutcomeMode
{
    /// <summary>Crits and damage use their expected (average) values; procs still roll.</summary>
    Expected,
    /// <summary>Crits roll and damage varies uniformly within +/-10%.</summary>
    Discrete
}

public class RandomOutcomeSource : IOutcomeSource
{
    // Game damage variance is +/- 10% (uniform random in [0.90, 1.10]).
    public const double DamageSpread = 0.10;

    private readonly SimulationRng _rng;

    public RandomOutcomeSource(SimulationRng rng, OutcomeMode mode = OutcomeMode.Expected)
    {
        _rng = rng;
        Mode = mode;
    }

    public OutcomeMode Mode { get; set; }

    public bool RollProc(string procKey, double chance, SimulationContext ctx, int evidenceLogId = 0, double effectDelaySeconds = 0.0, EnemyTarget? evidenceTarget = null) => _rng.Roll(chance);

    public IReadOnlyList<EnemyTarget> SelectTargets(int damageLogId, IReadOnlyList<EnemyTarget> candidates, SimulationContext ctx) => candidates;

    public EnemyTarget? ChooseTarget(int damageLogId, IReadOnlyList<EnemyTarget> candidates, SimulationContext ctx, EnemyTarget? preferred = null) =>
        preferred ?? (candidates.Count == 0 ? null : candidates[(int)(_rng.NextDouble() * candidates.Count) % candidates.Count]);

    public HitOutcome ResolveHit(in HitRequest hit, SimulationContext ctx)
    {
        if (Mode == OutcomeMode.Discrete)
        {
            bool isCrit = hit.CanCrit && _rng.Roll(hit.CritChance);
            double spread = (1.0 - DamageSpread) + (_rng.NextDouble() * 2.0 * DamageSpread);
            return new HitOutcome(isCrit, SimTime.Zero, spread);
        }
        return new HitOutcome(hit.CritChance > 0.5);
    }

    public double Land(in HitRequest landed, HitOutcome outcome, SimulationContext ctx)
    {
        if (Mode == OutcomeMode.Discrete) return (outcome.IsCrit ? landed.CritMultiplier : 1.0) * outcome.Spread;
        return landed.CanCrit ? 1.0 + (landed.CritChance * (landed.CritMultiplier - 1.0)) : 1.0;
    }

    public void HitFinished(SimulationContext ctx) { }
}

public class SimulationRng
{
    private Random _random;

    public SimulationRng(int seed = 42)
    {
        _random = new Random(seed);
    }

    public void Seed(int seed)
    {
        _random = new Random(seed);
    }

    public bool Roll(double chance) => _random.NextDouble() < chance;
    public double NextDouble() => _random.NextDouble();
}
