namespace FellowshipSim.Engine.Replay;

public static class EnemyRemovalReasonText
{
    /// <summary>The text of <paramref name="reason"/> in a replay script.</summary>
    public static string LogText(this EnemyRemovalReason reason) => reason switch
    {
        EnemyRemovalReason.Death => "death",
        EnemyRemovalReason.LastSeen => "last_seen",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };
}
