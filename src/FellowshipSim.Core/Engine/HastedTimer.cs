namespace FellowshipSim.Core.Engine;

/// <summary>
/// A one-shot timer that runs on hasted time: it fires after <c>baseSeconds</c> of progress, where progress runs at
/// (1 + haste) per second. When the hero's haste changes part way, the time left shrinks or grows from that moment
/// (combat logs: Amethyst Splinter ticks speed up as soon as Spirit of Heroism starts).
/// Haste is re-read whenever an aura is gained or removed and at the next aura expiry.
/// </summary>
public sealed class HastedTimer
{
    private readonly SimulationContext _ctx;
    private readonly Hero _hero;
    private readonly Action<SimulationContext> _onFire;
    private readonly string _label;
    private readonly double _baseSeconds;
    private double _remainingBase;
    private double _haste;
    private SimTime _lastUpdate;
    private ScheduledEvent? _event;
    private bool _updating;

    private HastedTimer(SimulationContext ctx, Hero hero, double baseSeconds, Action<SimulationContext> onFire, string label)
    {
        _ctx = ctx;
        _hero = hero;
        _onFire = onFire;
        _label = label;
        _baseSeconds = baseSeconds;
        _remainingBase = baseSeconds;
        _lastUpdate = ctx.CurrentTime;
        _haste = hero.GetHaste(ctx.CurrentTime);
    }

    public bool IsActive { get; private set; } = true;

    /// <summary>Hasted progress made so far, in base seconds.</summary>
    public double ElapsedBaseSeconds =>
        _baseSeconds - (_remainingBase - (_ctx.CurrentTime - _lastUpdate).TotalSeconds * (1.0 + _haste));

    public static HastedTimer Start(SimulationContext ctx, Hero hero, double baseSeconds, Action<SimulationContext> onFire, string label)
    {
        var timer = new HastedTimer(ctx, hero, baseSeconds, onFire, label);
        hero.Auras.OnGained += timer.OnAuraGained;
        hero.Auras.OnRemoved += timer.OnAuraRemoved;
        timer.Reschedule();
        return timer;
    }

    public void Cancel()
    {
        if (!IsActive) return;
        Finish();
    }

    private void OnAuraGained(Aura aura, int stacksBefore, SimTime now) => Update();
    private void OnAuraRemoved(Aura aura, SimTime now) => Update();

    /// <summary>Banks the progress made at the old haste, then re-reads haste and reschedules.</summary>
    private void Update()
    {
        if (!IsActive || _updating) return;
        _updating = true;
        try
        {
            var now = _ctx.CurrentTime;
            _remainingBase -= (now - _lastUpdate).TotalSeconds * (1.0 + _haste);
            _lastUpdate = now;
            double haste = _hero.GetHaste(now);
            if (Math.Abs(haste - _haste) < 1e-12 && _event is { HasFired: false, IsCancelled: false }) return;
            _haste = haste;
            Reschedule();
        }
        finally { _updating = false; }
    }

    private void Reschedule()
    {
        _event?.Cancel();
        var now = _ctx.CurrentTime;
        var due = now + SimTime.FromSeconds(Math.Max(0.0, _remainingBase) / (1.0 + _haste));
        var checkpoint = _hero.Auras.NextExpiry(now);
        if (checkpoint < due)
        {
            _event = _ctx.ScheduleAt(checkpoint, _ => Update(), $"{_label} haste check");
            return;
        }
        _event = _ctx.ScheduleAt(due, c =>
        {
            if (!IsActive) return;
            Finish();
            _onFire(c);
        }, _label);
    }

    private void Finish()
    {
        IsActive = false;
        _event?.Cancel();
        _hero.Auras.OnGained -= OnAuraGained;
        _hero.Auras.OnRemoved -= OnAuraRemoved;
    }
}
