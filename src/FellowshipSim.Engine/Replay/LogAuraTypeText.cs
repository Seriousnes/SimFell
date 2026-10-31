namespace FellowshipSim.Engine.Replay;

public static class LogAuraTypeText
{
    /// <summary>The text of <paramref name="type"/> in the combat log and a replay script.</summary>
    public static string LogText(this LogAuraType type) => type switch
    {
        LogAuraType.ApplyBuff => "applybuff",
        LogAuraType.ApplyBuffStack => "applybuffstack",
        LogAuraType.ApplyDebuff => "applydebuff",
        LogAuraType.ApplyDebuffStack => "applydebuffstack",
        LogAuraType.RefreshBuff => "refreshbuff",
        LogAuraType.RefreshDebuff => "refreshdebuff",
        LogAuraType.RemoveBuff => "removebuff",
        LogAuraType.RemoveBuffStack => "removebuffstack",
        LogAuraType.RemoveDebuff => "removedebuff",
        LogAuraType.RemoveDebuffStack => "removedebuffstack",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
