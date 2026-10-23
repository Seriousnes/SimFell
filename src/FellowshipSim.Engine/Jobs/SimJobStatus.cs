namespace FellowshipSim.Engine.Jobs;

/// <summary>A <see cref="SimJob"/>'s place in the <see cref="SimJobQueue"/>.</summary>
public enum SimJobStatus
{
    Queued,
    Running,
    Done,
    Failed,
    Cancelled,
}
