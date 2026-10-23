namespace FellowshipSim.Engine.Jobs;

/// <summary>How many jobs a <see cref="SimJobQueue"/> runs at once, and the threads they share.</summary>
public sealed record SimJobQueueOptions
{
    /// <summary>Jobs running at once; the rest stay queued.</summary>
    public int MaxConcurrentJobs { get; init; } = 2;

    /// <summary>Threads the running jobs share. Each running job has <c>max(1, ThreadBudget / MaxConcurrentJobs)</c>.</summary>
    public int ThreadBudget { get; init; } = MonteCarloSimulator.DefaultDegreeOfParallelism;

    /// <summary>The least time between two progress events of one job.</summary>
    public TimeSpan ProgressInterval { get; init; } = TimeSpan.FromMilliseconds(100);
}
