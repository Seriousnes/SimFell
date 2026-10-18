using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

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
        : base(owner, LogIds.Effect(CoalescingFrost.DamageEffectId), "Coalescing Frost")
    {
        BaseCoefficient = CoalescingFrost.CoefficientPerStack;
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
