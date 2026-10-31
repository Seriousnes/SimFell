namespace FellowshipSim.Engine.Replay;

public static class LogAuraTypeText
{
    public const string ApplyBuff = "applybuff";
    public const string ApplyBuffStack = "applybuffstack";
    public const string ApplyDebuff = "applydebuff";
    public const string ApplyDebuffStack = "applydebuffstack";
    public const string RefreshBuff = "refreshbuff";
    public const string RefreshDebuff = "refreshdebuff";
    public const string RemoveBuff = "removebuff";
    public const string RemoveBuffStack = "removebuffstack";
    public const string RemoveDebuff = "removedebuff";
    public const string RemoveDebuffStack = "removedebuffstack";

    /// <summary>The text of <paramref name="type"/> in the combat log and a replay script.</summary>
    public static string LogText(this LogAuraType type) => type switch
    {
        LogAuraType.ApplyBuff => ApplyBuff,
        LogAuraType.ApplyBuffStack => ApplyBuffStack,
        LogAuraType.ApplyDebuff => ApplyDebuff,
        LogAuraType.ApplyDebuffStack => ApplyDebuffStack,
        LogAuraType.RefreshBuff => RefreshBuff,
        LogAuraType.RefreshDebuff => RefreshDebuff,
        LogAuraType.RemoveBuff => RemoveBuff,
        LogAuraType.RemoveBuffStack => RemoveBuffStack,
        LogAuraType.RemoveDebuff => RemoveDebuff,
        LogAuraType.RemoveDebuffStack => RemoveDebuffStack,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
