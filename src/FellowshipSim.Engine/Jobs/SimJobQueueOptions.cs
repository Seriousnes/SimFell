namespace FellowshipSim.Engine.Jobs;

/// <summary>How many jobs a <see cref="SimJobQueue"/> runs at once, and the threads they share.</summary>
public sealed record SimJobQueueOptions
{
    /// <summary>Jobs running at once unless <see cref="MaxConcurrentJobs"/> says otherwise.</summary>
    public const int DefaultMaxConcurrentJobs = 2;

    /// <summary>Milliseconds between two progress events of one job unless <see cref="ProgressInterval"/> says otherwise.</summary>
    public const int DefaultProgressIntervalMilliseconds = 100;

    /// <summary>Jobs running at once; the rest stay queued.</summary>
    public int MaxConcurrentJobs { get; init; } = DefaultMaxConcurrentJobs;

    /// <summary>Threads the running jobs share. Each running job has <c>max(1, ThreadBudget / MaxConcurrentJobs)</c>.</summary>
    public int ThreadBudget { get; init; } = MonteCarloSimulator.DefaultDegreeOfParallelism;

    /// <summary>The least time between two progress events of one job.</summary>
    public TimeSpan ProgressInterval { get; init; } = TimeSpan.FromMilliseconds(DefaultProgressIntervalMilliseconds);
}
