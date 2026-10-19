namespace FellowshipSim.Logs.Replay;

/// <summary>
/// Free dice for a replay: the engine rolls its own crits, procs and damage spread, but where everyone stands comes from
/// the log. Hits that strike several enemies, or pick their own target, only reach the enemies the player was fighting:
/// those the log shows the player hitting (with anything) between the latest cast and the next one.
/// </summary>
public sealed class FreeDiceOutcomeSource : RandomOutcomeSource
{
    private readonly SimulationRng _rng;
    private readonly double[] _castTimes;
    private readonly HashSet<UnitRef>[] _engaged;

    public FreeDiceOutcomeSource(ReplayScript script, SimulationRng rng) : base(rng, OutcomeMode.Discrete)
    {
        _rng = rng;
        _castTimes = script.Commands.Where(c => c.IsPlayerAction).Select(c => c.Time).Distinct().Order().ToArray();
        // Window i runs from cast i to cast i + 1; window 0 also covers anything before the first cast.
        _engaged = new HashSet<UnitRef>[Math.Max(1, _castTimes.Length)];
        for (int i = 0; i < _engaged.Length; i++) _engaged[i] = new HashSet<UnitRef>();
        foreach (var hit in script.Observations.Damage)
        {
            _engaged[Window(hit.Time)].Add(hit.Target);
        }
    }

    private int Window(double time)
    {
        int i = Array.BinarySearch(_castTimes, time);
        if (i < 0) i = ~i - 1;
        return Math.Clamp(i, 0, _engaged.Length - 1);
    }

    private List<EnemyTarget> Engaged(IReadOnlyList<EnemyTarget> candidates, SimulationContext ctx)
    {
        var engaged = _engaged[Window(ctx.CurrentTime.TotalSeconds)];
        return candidates.Where(e => engaged.Contains(new UnitRef(e.Id, e.InstanceId))).ToList();
    }

    public override IReadOnlyList<EnemyTarget> SelectTargets(int damageLogId, IReadOnlyList<EnemyTarget> candidates, SimulationContext ctx) =>
        Engaged(candidates, ctx);

    public override EnemyTarget? ChooseTarget(int damageLogId, IReadOnlyList<EnemyTarget> candidates, SimulationContext ctx, EnemyTarget? preferred = null)
    {
        if (preferred != null) return preferred;
        var engaged = Engaged(candidates, ctx);
        return engaged.Count == 0 ? null : engaged[(int)(_rng.NextDouble() * engaged.Count) % engaged.Count];
    }
}
