namespace FellowshipSim.Engine.Replay;

public static class EnemyRemovalReasonText
{
    public const string Death = "death";
    public const string LastSeen = "last_seen";

    /// <summary>The text of <paramref name="reason"/> in a replay script.</summary>
    public static string LogText(this EnemyRemovalReason reason) => reason switch
    {
        EnemyRemovalReason.Death => Death,
        EnemyRemovalReason.LastSeen => LastSeen,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };
}
