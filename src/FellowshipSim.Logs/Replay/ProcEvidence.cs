using FellowshipSim.Core.Customization.Sets;
using FellowshipSim.Core.Customization.Traits;
using FellowshipSim.Core.Heroes.Rime.Abilities;
using FellowshipSim.Core.Heroes.Rime.Talents;

namespace FellowshipSim.Logs.Replay;

/// <summary>
/// What proves in a combat log that a proc happened, by the proc's name in the engine (<c>ProcKey</c>). A replay answers
/// each proc roll from the log: it procced if this aura or hit appears right after the roll. A proc not listed here is
/// treated as not procced and reported, so add procs here when adding them to the engine.
/// </summary>
public static class ProcEvidence
{
    /// <summary>
    /// <paramref name="LogId"/> is the aura the proc applies or the hit it deals; <paramref name="DelaySeconds"/> how long
    /// after the roll it may appear (e.g. a projectile's travel).
    /// </summary>
    public sealed record Evidence(string ProcKey, int LogId, double DelaySeconds = 0.0);

    public static readonly Evidence[] All =
    [
        new(Hero.SpiritRefundProc, Hero.RisingSpirit.LogId),
        new(DarkProphecy.ProcKey, DarkProphecy.Buff.LogId),
        // The spike erupts from the ground a moment after the proc (logs: about 0.5s).
        new(EmeraldJudgement.ProcKey, LogIds.Effect(EmeraldJudgement.DamageEffectId), DelaySeconds: 1.0),

        // Rime
        new(BurstingSwallows.ProcKey, LogIds.Effect(BurstingIce.PulseEffectId)),
        new(FrostweaversWrath.ProcKey, FrostweaversWrath.Buff.LogId),
        new(SoulfrostTorrent.ProcKey, SoulfrostTorrent.Buff.LogId),
    ];

    private static readonly Dictionary<string, Evidence> ByKey = All.ToDictionary(e => e.ProcKey);

    public static Evidence? For(string procKey) => ByKey.GetValueOrDefault(procKey);
}
