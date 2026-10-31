namespace FellowshipSim.Engine.Replay;

public static class LogEventTypes
{
    /// <summary>The member for a combat log event's <c>type</c> string; <see cref="LogEventType.Other"/> for a type the builder does not compare.</summary>
    public static LogEventType Parse(string type) => type switch
    {
        "begincast" => LogEventType.BeginCast,
        "cast" => LogEventType.Cast,
        "beginchannel" => LogEventType.BeginChannel,
        "endchannel" => LogEventType.EndChannel,
        "damage" => LogEventType.Damage,
        "death" => LogEventType.Death,
        "resurrect" => LogEventType.Resurrect,
        "combatantinfo" => LogEventType.CombatantInfo,
        "dungeonstart" => LogEventType.DungeonStart,
        "applybuff" => LogEventType.ApplyBuff,
        "applybuffstack" => LogEventType.ApplyBuffStack,
        "applydebuff" => LogEventType.ApplyDebuff,
        "applydebuffstack" => LogEventType.ApplyDebuffStack,
        "refreshbuff" => LogEventType.RefreshBuff,
        "refreshdebuff" => LogEventType.RefreshDebuff,
        "removebuff" => LogEventType.RemoveBuff,
        "removebuffstack" => LogEventType.RemoveBuffStack,
        "removedebuff" => LogEventType.RemoveDebuff,
        "removedebuffstack" => LogEventType.RemoveDebuffStack,
        _ => LogEventType.Other,
    };
}
