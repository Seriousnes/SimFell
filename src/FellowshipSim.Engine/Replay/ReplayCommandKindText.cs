namespace FellowshipSim.Engine.Replay;

public static class ReplayCommandKindText
{
    /// <summary>The text of <paramref name="kind"/> in a replay script, a review page and a command's description.</summary>
    public static string LogText(this ReplayCommandKind kind) => kind switch
    {
        ReplayCommandKind.EnemyArise => "enemy_arise",
        ReplayCommandKind.EnemyDemise => "enemy_demise",
        ReplayCommandKind.EnemyHealth => "enemy_health",
        ReplayCommandKind.ExternalAura => "external_aura",
        ReplayCommandKind.Cast => "cast",
        ReplayCommandKind.BeginCast => "begin_cast",
        ReplayCommandKind.CompleteCast => "complete_cast",
        ReplayCommandKind.CancelCast => "cancel_cast",
        ReplayCommandKind.StopChannel => "stop_channel",
        ReplayCommandKind.PlayerDeath => "player_death",
        ReplayCommandKind.PlayerRevive => "player_revive",
        ReplayCommandKind.BeginChannel => "begin_channel",
        ReplayCommandKind.EndChannel => "end_channel",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
