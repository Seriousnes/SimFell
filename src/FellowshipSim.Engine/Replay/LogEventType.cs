namespace FellowshipSim.Engine.Replay;

/// <summary>The combat log event types the replay script builder compares; <see cref="Other"/> is every other type.</summary>
public enum LogEventType
{
    Other,
    BeginCast,
    Cast,
    BeginChannel,
    EndChannel,
    Damage,
    Death,
    Resurrect,
    CombatantInfo,
    DungeonStart,
    ApplyBuff,
    ApplyBuffStack,
    ApplyDebuff,
    ApplyDebuffStack,
    RefreshBuff,
    RefreshDebuff,
    RemoveBuff,
    RemoveBuffStack,
    RemoveDebuff,
    RemoveDebuffStack,
}
