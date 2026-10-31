namespace FellowshipSim.Engine.Replay;

public static class ReplayCommandKindText
{
    public const string EnemyArise = "enemy_arise";
    public const string EnemyDemise = "enemy_demise";
    public const string EnemyHealth = "enemy_health";
    public const string ExternalAura = "external_aura";
    public const string Cast = "cast";
    public const string BeginCast = "begin_cast";
    public const string CompleteCast = "complete_cast";
    public const string CancelCast = "cancel_cast";
    public const string StopChannel = "stop_channel";
    public const string PlayerDeath = "player_death";
    public const string PlayerRevive = "player_revive";
    public const string BeginChannel = "begin_channel";
    public const string EndChannel = "end_channel";

    /// <summary>The text of <paramref name="kind"/> in a replay script, a review page and a command's description.</summary>
    public static string LogText(this ReplayCommandKind kind) => kind switch
    {
        ReplayCommandKind.EnemyArise => EnemyArise,
        ReplayCommandKind.EnemyDemise => EnemyDemise,
        ReplayCommandKind.EnemyHealth => EnemyHealth,
        ReplayCommandKind.ExternalAura => ExternalAura,
        ReplayCommandKind.Cast => Cast,
        ReplayCommandKind.BeginCast => BeginCast,
        ReplayCommandKind.CompleteCast => CompleteCast,
        ReplayCommandKind.CancelCast => CancelCast,
        ReplayCommandKind.StopChannel => StopChannel,
        ReplayCommandKind.PlayerDeath => PlayerDeath,
        ReplayCommandKind.PlayerRevive => PlayerRevive,
        ReplayCommandKind.BeginChannel => BeginChannel,
        ReplayCommandKind.EndChannel => EndChannel,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };
}
