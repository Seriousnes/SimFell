using System.Diagnostics;

namespace FellowshipSim.Engine.Jobs;

/// <summary>
/// Sims and reviews run in the background, <see cref="SimJobQueueOptions.MaxConcurrentJobs"/> at once in the order they
/// were enqueued; the rest wait as <see cref="SimJobStatus.Queued"/>. Each running job's work runs on a thread-pool thread
/// with its share of <see cref="SimJobQueueOptions.ThreadBudget"/>. Every member is safe to call from any thread.
/// </summary>
public sealed class SimJobQueue
{
    private readonly Lock _lock = new();
    private readonly List<SimJob> _jobs = [];
    private readonly LinkedList<SimJob> _queued = [];
    private int _running;

    public SimJobQueue(SimJobQueueOptions? options = null)
    {
        Options = options ?? new SimJobQueueOptions();
        if (Options.MaxConcurrentJobs < 1) throw new ArgumentOutOfRangeException(nameof(options), "MaxConcurrentJobs is at least 1.");
    }

    public SimJobQueueOptions Options { get; }

    /// <summary>Threads each running job has: <c>max(1, ThreadBudget / MaxConcurrentJobs)</c>.</summary>
    public int JobDegreeOfParallelism => Math.Max(1, Options.ThreadBudget / Options.MaxConcurrentJobs);

    /// <summary>
    /// A job was enqueued, removed, or changed. A status change raises it at once; progress raises it at most once per
    /// <see cref="SimJobQueueOptions.ProgressInterval"/> per job, and the latest progress always raises it. It is raised on
    /// a job thread or the caller's thread: a Blazor component handles it through <c>InvokeAsync</c>.
    /// </summary>
    public event Action<SimJob>? Changed;

    /// <summary>
    /// Queues <paramref name="work"/> and returns its job. The work's result becomes <see cref="SimJob.Result"/>. An
    /// exception makes the job <see cref="SimJobStatus.Failed"/>, and an <see cref="OperationCanceledException"/> of the
    /// job's own token makes it <see cref="SimJobStatus.Cancelled"/>.
    /// </summary>
    public SimJob Enqueue<T>(SimJobKind kind, string title, Func<SimJobContext, Task<T>> work, object? input = null)
    {
        var job = new SimJob(kind, title, input, async context => await work(context));
        lock (_lock)
        {
            _jobs.Add(job);
            _queued.AddLast(job);
        }
        Raise(job);
        StartQueued();
        return job;
    }

    /// <summary>Every job not removed, oldest first.</summary>
    public IReadOnlyList<SimJob> List()
    {
        lock (_lock) return _jobs.ToList();
    }

    public SimJob? Get(string id)
    {
        lock (_lock) return _jobs.FirstOrDefault(j => j.Id == id);
    }

    /// <summary>
    /// Cancels a queued or running job. A queued job is <see cref="SimJobStatus.Cancelled"/> at once. A running job's token
    /// is cancelled: the job is cancelled when its work throws for the token, and work that ignores the token and ends
    /// leaves the job done. False when no unfinished job has the id.
    /// </summary>
    public bool Cancel(string id)
    {
        SimJob? job;
        lock (_lock)
        {
            job = _jobs.FirstOrDefault(j => j.Id == id);
            if (job == null || job.IsFinished) return false;
            if (job.Status == SimJobStatus.Queued)
            {
                _queued.Remove(job);
                Finish(job, SimJobStatus.Cancelled);
            }
        }
        job.Cancellation.Cancel();
        Raise(job);
        return true;
    }

    /// <summary>Removes a job from the list, cancelling it first when it is queued or running. False when no job has the id.</summary>
    public bool Remove(string id)
    {
        Cancel(id);
        SimJob? job;
        lock (_lock)
        {
            job = _jobs.FirstOrDefault(j => j.Id == id);
            if (job == null) return false;
            _jobs.Remove(job);
        }
        Raise(job);
        return true;
    }

    internal void Report(SimJob job, double? fraction, string? message)
    {
        if (job.IsFinished) return;
        if (fraction is { } f) job.Progress = Math.Max(job.Progress ?? 0, Math.Clamp(f, 0, 1));
        if (message != null) job.Message = message;
        RaiseThrottled(job);
    }

    private void StartQueued()
    {
        while (true)
        {
            SimJob job;
            lock (_lock)
            {
                if (_running >= Options.MaxConcurrentJobs || _queued.First is not { } first) return;
                job = first.Value;
                _queued.RemoveFirst();
                _running++;
                job.Status = SimJobStatus.Running;
                job.StartedAt = DateTime.UtcNow;
            }
            Raise(job);
            _ = Task.Run(() => RunAsync(job));
        }
    }

    private async Task RunAsync(SimJob job)
    {
        var context = new SimJobContext(this, job, JobDegreeOfParallelism);
        SimJobStatus status;
        object? result = null;
        string? error = null;
        try
        {
            result = await job.Work(context);
            status = SimJobStatus.Done;
        }
        catch (OperationCanceledException) when (job.Cancellation.IsCancellationRequested)
        {
            status = SimJobStatus.Cancelled;
        }
        catch (Exception e)
        {
            status = SimJobStatus.Failed;
            error = e is SimEngineException or FellowshipLogsException ? e.Message : $"{e.GetType().Name}: {e.Message}";
        }

        lock (_lock)
        {
            _running--;
            job.Result = result;
            job.Error = error;
            if (status == SimJobStatus.Done) job.Progress = 1;
            Finish(job, status);
        }
        Raise(job);
        StartQueued();
    }

    private static void Finish(SimJob job, SimJobStatus status)
    {
        job.Status = status;
        job.FinishedAt = DateTime.UtcNow;
    }

    private void RaiseThrottled(SimJob job)
    {
        var elapsed = Stopwatch.GetElapsedTime(Volatile.Read(ref job.LastRaised));
        if (elapsed >= Options.ProgressInterval)
        {
            Raise(job);
            return;
        }
        if (Interlocked.Exchange(ref job.RaisePending, 1) == 1) return;
        _ = Task.Delay(Options.ProgressInterval - elapsed).ContinueWith(_ =>
        {
            Volatile.Write(ref job.RaisePending, 0);
            Raise(job);
        }, TaskScheduler.Default);
    }

    private void Raise(SimJob job)
    {
        Volatile.Write(ref job.LastRaised, Stopwatch.GetTimestamp());
        if (Changed is not { } changed) return;
        // A failing subscriber stops neither the job nor the other subscribers.
        foreach (var handler in changed.GetInvocationList().Cast<Action<SimJob>>())
        {
            try
            {
                handler(job);
            }
            catch (Exception)
            {
            }
        }
    }
}
