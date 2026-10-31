namespace FellowshipSim.Engine.Replay;

public static class LogEventTypes
{
    public const string BeginCast = "begincast";
    public const string Cast = "cast";
    public const string BeginChannel = "beginchannel";
    public const string EndChannel = "endchannel";
    public const string Damage = "damage";
    public const string Heal = "heal";
    public const string Death = "death";
    public const string Resurrect = "resurrect";
    public const string CombatantInfo = "combatantinfo";
    public const string DungeonStart = "dungeonstart";

    /// <summary>The member for a combat log event's <c>type</c> string; <see cref="LogEventType.Other"/> for a type the builder does not compare.</summary>
    public static LogEventType Parse(string type) => type switch
    {
        BeginCast => LogEventType.BeginCast,
        Cast => LogEventType.Cast,
        BeginChannel => LogEventType.BeginChannel,
        EndChannel => LogEventType.EndChannel,
        Damage => LogEventType.Damage,
        Death => LogEventType.Death,
        Resurrect => LogEventType.Resurrect,
        CombatantInfo => LogEventType.CombatantInfo,
        DungeonStart => LogEventType.DungeonStart,
        LogAuraTypeText.ApplyBuff => LogEventType.ApplyBuff,
        LogAuraTypeText.ApplyBuffStack => LogEventType.ApplyBuffStack,
        LogAuraTypeText.ApplyDebuff => LogEventType.ApplyDebuff,
        LogAuraTypeText.ApplyDebuffStack => LogEventType.ApplyDebuffStack,
        LogAuraTypeText.RefreshBuff => LogEventType.RefreshBuff,
        LogAuraTypeText.RefreshDebuff => LogEventType.RefreshDebuff,
        LogAuraTypeText.RemoveBuff => LogEventType.RemoveBuff,
        LogAuraTypeText.RemoveBuffStack => LogEventType.RemoveBuffStack,
        LogAuraTypeText.RemoveDebuff => LogEventType.RemoveDebuff,
        LogAuraTypeText.RemoveDebuffStack => LogEventType.RemoveDebuffStack,
        _ => LogEventType.Other,
    };
}
