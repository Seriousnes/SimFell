using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Core.Heroes.Rime.Abilities;

/// <summary>Ability 1027. Channel: ticks every 0.4s (hasted) for 2s, 1 Anima per tick.</summary>
public class FreezingTorrent : RimeAbility
{
    public const int AbilityId = 1027;
    public const double AnimaPerTick = 1.0;

    public FreezingTorrent(Rime owner) : base(owner, AbilityId)
    {
        // Combat logs: a tick lands as the channel starts, then every hasted period, plus a partial final tick.
        ChannelTicksOnStart = true;
        ChannelPartialFinalTick = true;
    }

    /// <summary>Supreme Torrent: the channel lasts longer.</summary>
    public void ExtendChannel(double seconds) => ChannelDuration += SimTime.FromSeconds(seconds);

    private bool _soulfrost;

    /// <summary>Soulfrost Torrent: the channel it empowers ticks 40% faster.</summary>
    public override double ChannelTickPeriodFor(SimulationContext ctx) =>
        _soulfrost ? ChannelTickPeriodSeconds / SoulfrostTorrent.TickRateMultiplier : ChannelTickPeriodSeconds;

    public override void Execute(SimulationContext ctx, EnemyTarget target)
    {
        TriggerCooldown(ctx);
        _soulfrost = Rime.Auras.HasAura(Rime.Aura.SoulfrostTorrent, ctx.CurrentTime);
        if (_soulfrost) Rime.Auras.Remove(Rime.Aura.SoulfrostTorrent, ctx.CurrentTime);
    }

    public override void OnChannelEnd(SimulationContext ctx, EnemyTarget target, bool clipped) => _soulfrost = false;

    public override void OnChannelTick(SimulationContext ctx, EnemyTarget target, double fraction)
    {
        // Flight of the Navir: every tick (the partial one too) sends a swallow at an enemy of its choosing
        // (yMAZVJL9hTpFjqbC fight 7 @ 49.522: 10 ticks, 10 hits; @ 34.229: one per tick until Flight of the Navir ends).
        if (Rime.Auras.HasAura(Rime.Aura.FlightOfTheNavir, ctx.CurrentTime))
        {
            var swallows = Rime.GetAbility<CommandedFrostSwallows>();
            var enemy = swallows is not { Available: > 0 } ? null : ctx.Outcomes.ChooseTarget(swallows.DamageLogId, ctx.ActiveEnemies.ToList(), ctx);
            if (enemy != null) swallows!.Send(ctx, enemy);
        }
        Rime.AddAnima(AnimaPerTick, ctx, target);
        DealDamage(ctx, target, damageMultiplier: fraction, extraCritChance: _soulfrost ? SoulfrostTorrent.CritChance : 0.0);
        if (Rime.HasTalent<ColdShower>() && ctx.Outcomes.RollProc(ColdShower.ProcKey, ColdShower.ProcChance, ctx))
        {
            Rime.CastFreeIceComet(ctx, target);
        }
        if (Rime.HasTalent<ChillingFinesse>())
        {
            Rime.ReduceCooldown<BurstingIce>(SimTime.FromSeconds(ChillingFinesse.BurstingIceReductionSeconds), ctx.CurrentTime);
        }
    }
}
