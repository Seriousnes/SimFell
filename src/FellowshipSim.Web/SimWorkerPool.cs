using System.Reflection;
using FellowshipSim.Engine;
using FellowshipSim.UI.Services;
using Microsoft.JSInterop;

namespace FellowshipSim.Web;

/// <summary>
/// The page's side of the sim workers (sim/simPool.js): a sim's chunks spread over them, and a replay on one of them. A
/// worker's error comes back as a <see cref="SimEngineException"/>; a cancelled sim as an
/// <see cref="OperationCanceledException"/>.
/// </summary>
public sealed class SimWorkerPool(IJSRuntime js)
{
    /// <summary>Key of the assembly metadata that holds the build's stamp.</summary>
    private const string BuildStampKey = "BuildStamp";

    /// <summary>Stamp of a build made without one.</summary>
    private const string DefaultBuildStamp = "0";

    /// <summary>Function of the page that loads a module.</summary>
    private const string ImportFunction = "import";

    /// <summary>Module of the page's side of the sim workers.</summary>
    private const string SimPoolModule = "./sim/simPool.js";

    private const string SizeFunction = "size";
    private const string CoresFunction = "cores";
    private const string CancelFunction = "cancel";
    private const string SimulateFunction = "simulate";
    private const string ReviewFunction = "review";
    private const string ReviewFightFunction = "reviewFight";

    /// <summary>The prefix a JS error's message has.</summary>
    private const string ErrorPrefix = "Error: ";

    private IJSObjectReference? _pool;
    private int _nextRun;

    /// <summary>
    /// The build's stamp (the project's BuildStamp). The sim scripts' addresses carry it, so a browser never runs a
    /// script it kept from an older build (GitHub Pages lets it keep one for 10 minutes) with this build's app.
    /// </summary>
    private static readonly string BuildStamp = typeof(SimWorkerPool).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == BuildStampKey)?.Value ?? DefaultBuildStamp;

    private async Task<IJSObjectReference> PoolAsync() =>
        _pool ??= await js.InvokeAsync<IJSObjectReference>(ImportFunction, $"{SimPoolModule}?v={BuildStamp}");

    /// <summary>The workers a sim can use: the cores but the page's, up to the pool's most.</summary>
    public async Task<int> SizeAsync() => await (await PoolAsync()).InvokeAsync<int>(SizeFunction);

    /// <summary>The browser's logical processors.</summary>
    public async Task<int> CoresAsync() => await (await PoolAsync()).InvokeAsync<int>(CoresFunction);

    /// <summary>
    /// Runs a sim's <paramref name="chunkCount"/> chunks on up to <paramref name="workers"/> workers (all when null) and
    /// returns its result's JSON. <paramref name="chunkDone"/> hears the count of chunks done as each completes.
    /// </summary>
    public async Task<string> SimulateAsync(string profileJson, string settingsJson, int chunkCount, int? workers,
        Action<int> chunkDone, CancellationToken ct)
    {
        var pool = await PoolAsync();
        var run = Interlocked.Increment(ref _nextRun);
        using var sink = DotNetObjectReference.Create(new Sink(chunkDone, null));
        await using var stop = ct.Register(() => _ = pool.InvokeVoidAsync(CancelFunction, run).AsTask());
        try
        {
            return await pool.InvokeAsync<string>(SimulateFunction, CancellationToken.None, run, profileJson, settingsJson, chunkCount, workers, sink);
        }
        catch (JSException) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(ct);
        }
        catch (JSException exception)
        {
            throw new SimEngineException(Reason(exception));
        }
    }

    /// <summary>Replays player <paramref name="playerId"/> of a raw fight on a worker; the worker's <see cref="ReviewReply"/> JSON.</summary>
    public async Task<string> ReviewAsync(string rawFight, int playerId, IProgress<string>? progress)
    {
        var pool = await PoolAsync();
        using var sink = DotNetObjectReference.Create(new Sink(null, progress));
        try
        {
            return await pool.InvokeAsync<string>(ReviewFunction, CancellationToken.None, rawFight, playerId, sink);
        }
        catch (JSException exception)
        {
            throw new SimEngineException(Reason(exception));
        }
    }

    /// <summary>
    /// Downloads a fight and replays player <paramref name="playerId"/> on a worker, as the signed-in user
    /// (<paramref name="accessToken"/>) or else with a developer's <paramref name="credentials"/>; the worker's
    /// <see cref="ReviewReply"/> JSON.
    /// </summary>
    public async Task<string> ReviewFightAsync(string reportCode, int fightId, int playerId, string? accessToken, LogsCredentials? credentials,
        IProgress<string>? progress)
    {
        var pool = await PoolAsync();
        using var sink = DotNetObjectReference.Create(new Sink(null, progress));
        try
        {
            return await pool.InvokeAsync<string>(ReviewFightFunction, CancellationToken.None, reportCode, fightId, playerId,
                accessToken ?? "", credentials?.ClientId ?? "", credentials?.ClientSecret ?? "", sink);
        }
        catch (JSException exception)
        {
            throw new SimEngineException(Reason(exception));
        }
    }

    // The first line of a worker's error, without the JS error's own prefix.
    private static string Reason(JSException exception)
    {
        var line = exception.Message.Split('\n')[0].Trim();
        return line.StartsWith(ErrorPrefix, StringComparison.Ordinal) ? line[ErrorPrefix.Length..] : line;
    }

    /// <summary>What simPool.js tells the page as a run goes: chunks done, or a replay's progress lines.</summary>
    public sealed class Sink(Action<int>? chunkDone, IProgress<string>? progress)
    {
        [JSInvokable]
        public void OnChunk(int done) => chunkDone?.Invoke(done);

        [JSInvokable]
        public void OnProgress(string text) => progress?.Report(text);
    }
}
