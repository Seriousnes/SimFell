using FellowshipSim.UI.Services;
using FellowshipSim.Logs;
using Microsoft.JSInterop;

namespace FellowshipSim.Web;

/// <summary>
/// The browser app's data folder. Its files are in the browser runtime's in-memory file system at <see cref="Root"/>, so
/// every store that reads and writes the folder (reviews, settings, builds, action lists, sims) runs as it does on a
/// computer; storage.js keeps a copy of each file in the browser's IndexedDB. The copies come back at start
/// (<see cref="RestoreAsync"/>), and a changed or removed file is copied every couple of seconds (<see cref="KeepSaving"/>).
/// The browser keeps the newest <see cref="KeptSims"/> sims (<see cref="FellowshipSim.UI.Services.SimResultLibrary.Keep"/>)
/// and <see cref="KeptReviews"/> reviews. It keeps no downloaded fight and no replay script here (<see cref="NotKept"/>): a
/// fight is hundreds of MB once read, so a sim worker downloads and replays it (<see cref="WorkerFightReviewer"/>), and
/// keeps the newest fights in a store of its own that the page never loads (sim/fights.js).
/// </summary>
public sealed class BrowserDataFolder(IJSRuntime jsRuntime)
{
    public const string Root = "/simfell";

    public const int KeptSims = 20;

    /// <summary>Reviews kept: each page is several MB, all of it in the page's memory.</summary>
    public const int KeptReviews = 10;

    /// <summary>Module that copies the files to the browser's IndexedDB.</summary>
    private const string StorageModule = "./storage.js";

    /// <summary>The FellowshipLogs credentials file. The browser's storage does not keep it.</summary>
    private const string SettingsFile = "settings.json";

    private const string ListFunction = "list";
    private const string ReadFunction = "read";
    private const string WriteFunction = "write";
    private const string RemoveFunction = "remove";


    /// <summary>
    /// What the browser keeps nothing of: raw fights, replay scripts, and an API client a visitor saved before the app
    /// stopped taking one.
    /// </summary>
    private static readonly string[] NotKept = [$"{ReplayLibrary.FightsFolder}/", $"{ReplayLibrary.ScriptsFolder}/", SettingsFile];

    private static readonly TimeSpan SaveEvery = TimeSpan.FromSeconds(2);

    // Each file copied, by its path under Root: its length and when it was written, as copied.
    private readonly Dictionary<string, FileStamp> _copied = new(StringComparer.Ordinal);
    private IJSObjectReference? _storage;

    private async Task<IJSObjectReference> StorageAsync() =>
        _storage ??= await jsRuntime.InvokeAsync<IJSObjectReference>(JsInterop.ImportFunction, StorageModule);

    /// <summary>Writes every file the browser keeps into the data folder.</summary>
    public async Task RestoreAsync()
    {
        Directory.CreateDirectory(Root);
        var storage = await StorageAsync();
        foreach (var key in await storage.InvokeAsync<string[]>(ListFunction))
        {
            // An earlier version kept fights: they go without being read.
            if (NotKept.Any(key.StartsWith))
            {
                await storage.InvokeVoidAsync(RemoveFunction, key);
                continue;
            }
            if (await storage.InvokeAsync<string?>(ReadFunction, key) is not { } text || PathOf(key) is not { } path) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, text);
            _copied[key] = Stamp(path);
        }
    }

    /// <summary>Copies the folder's changes to the browser's storage from now on, every <see cref="SaveEvery"/>.</summary>
    public void KeepSaving() => _ = SaveLoopAsync();

    private async Task SaveLoopAsync()
    {
        while (true)
        {
            await Task.Delay(SaveEvery);
            try
            {
                await SaveAsync();
            }
            catch (Exception exception) when (exception is IOException or JSException)
            {
                Console.Error.WriteLine($"SimFell could not save its files in the browser: {exception.Message}");
            }
        }
    }

    /// <summary>Drops what the browser does not keep, then copies each new or changed file and forgets each removed one.</summary>
    public async Task SaveAsync()
    {
        Prune();
        var storage = await StorageAsync();
        var present = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
        {
            var key = Path.GetRelativePath(Root, path).Replace('\\', '/');
            present.Add(key);
            var stamp = Stamp(path);
            if (_copied.TryGetValue(key, out var copied) && copied == stamp) continue;
            await storage.InvokeVoidAsync(WriteFunction, key, await File.ReadAllTextAsync(path));
            _copied[key] = stamp;
        }
        foreach (var key in _copied.Keys.Where(k => !present.Contains(k)).ToList())
        {
            await storage.InvokeVoidAsync(RemoveFunction, key);
            _copied.Remove(key);
        }
    }

    // No fight or replay script stays (a review writes its script); the newest reviews stay, the rest go.
    private static void Prune()
    {
        foreach (var path in NotKept.Select(p => Path.Combine(Root, p)))
        {
            if (File.Exists(path)) File.Delete(path);
            else if (Directory.Exists(path)) foreach (var file in new DirectoryInfo(path).GetFiles()) file.Delete();
        }
        var reviews = new DirectoryInfo(Path.Combine(Root, ReplayLibrary.ReviewsFolder));
        if (!reviews.Exists) return;
        foreach (var old in reviews.GetFiles(ReplayLibrary.SummaryPattern).OrderByDescending(f => f.LastWriteTimeUtc).Skip(KeptReviews))
        {
            var id = old.Name[..^ReplayLibrary.SummaryExtension.Length];
            foreach (var file in reviews.GetFiles($"{id}.*")) file.Delete();
        }
    }

    private static FileStamp Stamp(string path)
    {
        var file = new FileInfo(path);
        return new FileStamp(file.Length, file.LastWriteTimeUtc);
    }

    // A storage key as a path under Root; null for one that would leave it.
    private static string? PathOf(string key)
    {
        var path = Path.GetFullPath(Path.Combine(Root, key));
        return path.StartsWith(Root + "/", StringComparison.Ordinal) ? path : null;
    }

    private readonly record struct FileStamp(long Length, DateTime Written);
}
