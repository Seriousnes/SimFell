// Downloads FellowshipLogs fights of one hero, from the patch the engine models (GamePatch.Current), into data/fights.
//
// The fights come from the hero's ranking lists in the latest zone, from rank 1 down, one encounter after another, so the
// set spans the zone's dungeons. A run is taken when it started on or after the patch's release day, its report has the
// patch's log format, and its fight has a player of the hero. A fight already in data/fights counts and is not
// downloaded again.
//
//     dotnet run tools/fetch-fights.cs -- --hero Rime [--count 12] [--max-per-player 2] [--pages 10]
//
// Credentials are FELLOWSHIPLOGS_CLIENT_ID and FELLOWSHIPLOGS_CLIENT_SECRET, from the environment or the simfell user
// secrets (dotnet user-secrets set FELLOWSHIPLOGS_CLIENT_ID <id> --id simfell).
#:project ../src/FellowshipSim.Logs/FellowshipSim.Logs.csproj
#:package Microsoft.Extensions.Configuration.EnvironmentVariables@10.0.13
#:package Microsoft.Extensions.Configuration.UserSecrets@10.0.13
#:property PublishAot=false
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using FellowshipSim.Logs;
using Microsoft.Extensions.Configuration;
using static Names;

string root = Path.GetDirectoryName((string)AppContext.GetData("EntryPointFileDirectoryPath")!)!;
string? hero = null;
int count = DefaultCount, maxPerPlayer = DefaultMaxPerPlayer, maxPages = DefaultMaxPages;
for (int i = 0; i < args.Length; i++)
{
    string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value.");
    switch (args[i])
    {
        case HeroOption: hero = Next(); break;
        case CountOption: count = int.Parse(Next()); break;
        case MaxPerPlayerOption: maxPerPlayer = int.Parse(Next()); break;
        case PagesOption: maxPages = int.Parse(Next()); break;
        default: Console.Error.WriteLine($"Unknown option {args[i]}."); return 1;
    }
}
if (hero == null)
{
    Console.Error.WriteLine("Usage: dotnet run tools/fetch-fights.cs -- --hero <Hero> [--count 12] [--max-per-player 2] [--pages 10]");
    return 1;
}

var credentials = Credentials();
if (credentials == null)
{
    Console.Error.WriteLine("No FellowshipLogs credentials: set FELLOWSHIPLOGS_CLIENT_ID and FELLOWSHIPLOGS_CLIENT_SECRET " +
                            "in the environment or with dotnet user-secrets set <key> <value> --id simfell.");
    return 1;
}

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(HttpTimeoutMinutes) };
var client = new FellowshipLogsClient(http, credentials.Value.Id, credentials.Value.Secret);
var library = new ReplayLibrary(Path.Combine(root, DataFolder));
Directory.CreateDirectory(library.FightsDirectory);
var patch = GamePatch.Current;
long releasedMs = new DateTimeOffset(patch.Released.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUnixTimeMilliseconds();

var world = await client.QueryAsync(WorldQuery);
var zone = world[WorldDataProperty]![ZonesProperty]!.AsArray().MaxBy(z => (int)z![IdProperty]!)!;
var encounters = zone[EncountersProperty]!.AsArray().Select(e => new ZoneEncounter((int)e![IdProperty]!, (string)e[NameProperty]!)).ToList();
var spec = world[GameDataProperty]![ClassesProperty]!.AsArray()
    .SelectMany(c => c![SpecsProperty]!.AsArray().Select(s => new HeroSpec((string)c[SlugProperty]!, (string)s![NameProperty]!, (string)s[SlugProperty]!)))
    .FirstOrDefault(s => s.Name.Equals(hero, StringComparison.OrdinalIgnoreCase));
if (spec.Name == null)
{
    Console.Error.WriteLine($"FellowshipLogs has no hero named {hero}.");
    return 1;
}
Console.WriteLine($"Zone {(int)zone[IdProperty]!} \"{(string)zone[NameProperty]!}\", {encounters.Count} encounters; {spec.Name} runs of patch {patch.Name} " +
                  $"(from {patch.Released:yyyy-MM-dd}, log format {patch.LogVersion}).");

var taken = new List<TakenFight>();
var skipped = new Dictionary<string, int>();
var queues = encounters.Select(e => new RankingQueue(e.Id, e.Name)).ToList();
var reports = new Dictionary<string, ReportSummary>();
while (taken.Count < count && queues.Any(q => !q.Exhausted))
{
    foreach (var queue in queues.Where(q => !q.Exhausted))
    {
        if (taken.Count >= count) break;
        while (await queue.NextAsync(client, spec.Class, spec.Slug, maxPages) is { } run)
        {
            string? reason = await Reject(run);
            if (reason == null) break;
            skipped[reason] = skipped.GetValueOrDefault(reason) + 1;
        }
    }
}

Console.WriteLine($"{taken.Count} of {count} fights wanted.");
foreach (var group in taken.GroupBy(t => t.Encounter)) Console.WriteLine($"  {group.Key}: {group.Count()}");
foreach (var (reason, reasonCount) in skipped.OrderByDescending(s => s.Value)) Console.WriteLine($"  skipped, {reason}: {reasonCount}");
if (taken.Count < count)
{
    Console.WriteLine($"The ranking lists hold {taken.Count} usable runs of patch {patch.Name} within {maxPages} pages per encounter. " +
                      "A larger --pages reads further down the lists.");
}
return 0;

async Task<string?> Reject(JsonNode run)
{
    string? code = (string?)run[ReportProperty]?[CodeProperty];
    int? fightId = (int?)run[ReportProperty]?[FightIdProperty];
    string player = ((string?)run[NameProperty] ?? "").Trim();
    if (code == null || fightId == null) return "no report";
    if ((long?)run[StartTimeProperty] is not { } startMs || startMs < releasedMs) return "before the patch";
    if (taken.Any(t => t.Code == code && t.FightId == fightId)) return "fight already taken";
    if (taken.Count(t => t.Player.Equals(player, StringComparison.OrdinalIgnoreCase)) >= maxPerPlayer) return "player cap";

    if (!reports.TryGetValue(code, out var report))
    {
        try { report = reports[code] = await client.GetSummaryAsync(code); }
        catch (FellowshipLogsException exception) when (!Throttled(exception)) { return "report unavailable"; }
    }
    if (!patch.Covers(DateTimeOffset.FromUnixTimeMilliseconds(report.StartTime).UtcDateTime, report.LogVersion)) return "older log format";
    var fight = report.Fights.FirstOrDefault(f => f.Id == fightId);
    if (fight == null || !fight.Players.Any(p => p.Hero.Equals(spec.Name, StringComparison.OrdinalIgnoreCase))) return "hero not in the fight";

    await WaitForPointsAsync();
    try { await library.GetRawFightFileAsync(client, code, fightId.Value); }
    catch (FellowshipLogsException exception) when (!Throttled(exception)) { return "download failed"; }
    string encounter = queues.First(q => q.Holds(run)).Name;
    taken.Add(new TakenFight(encounter, player, code, fightId.Value));
    Console.WriteLine($"  {encounter}: {player} {code} fight {fightId} ({fight.Name}, {fight.DurationSeconds / SecondsPerMinute:F0} min)");
    return null;
}

// A fight's events cost FellowshipLogs points. Below this many left in the hour, the download waits for the reset.
async Task WaitForPointsAsync()
{
    const int Reserve = 600;
    var rate = (await client.QueryAsync(RateLimitQuery))[RateLimitDataProperty]!;
    double left = (double)rate[LimitPerHourProperty]! - (double)rate[PointsSpentThisHourProperty]!;
    if (left >= Reserve) return;
    int wait = (int)rate[PointsResetInProperty]! + ResetMarginSeconds;
    Console.WriteLine($"  {left:F0} FellowshipLogs points left this hour; waiting {wait}s for the reset.");
    await Task.Delay(TimeSpan.FromSeconds(wait));
}

// A refusal for the rate limit or a server error stops the run; it says nothing about the report.
static bool Throttled(FellowshipLogsException exception) =>
    exception.Message.Contains(RateLimitedText) || System.Text.RegularExpressions.Regex.IsMatch(exception.Message, ServerErrorPattern);

static ApiCredentials? Credentials()
{
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets(Assembly.GetEntryAssembly()!)
        .AddEnvironmentVariables()
        .Build();
    string? id = configuration[ClientIdVariable];
    string? secret = configuration[ClientSecretVariable];
    return !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(secret) ? new ApiCredentials(id, secret) : null;
}
/// <summary>One encounter's ranking list of the hero, read a page at a time from rank 1.</summary>
sealed class RankingQueue(int encounterId, string name)
{
    private readonly Queue<JsonNode> _runs = new();
    private readonly HashSet<JsonNode> _read = [];
    private int _page;
    private bool _lastPage;

    public string Name { get; } = name;
    public bool Exhausted => _lastPage && _runs.Count == 0;

    public bool Holds(JsonNode run) => _read.Contains(run);

    public async Task<JsonNode?> NextAsync(FellowshipLogsClient client, string classSlug, string specSlug, int maxPages)
    {
        if (_runs.Count == 0 && !_lastPage)
        {
            _page++;
            var data = await client.QueryAsync(
                $"{{ worldData {{ encounter(id: {encounterId}) {{ characterRankings(className: \"{classSlug}\", specName: \"{specSlug}\", page: {_page}) }} }} }}");
            var page = data[WorldDataProperty]![EncounterProperty]![CharacterRankingsProperty]!;
            foreach (var run in page[RankingsProperty]?.AsArray() ?? [])
            {
                if (run == null) continue;
                _runs.Enqueue(run);
                _read.Add(run);
            }
            _lastPage = (bool?)page[HasMorePagesProperty] != true || _page >= maxPages;
        }
        return _runs.TryDequeue(out var next) ? next : null;
    }
}

readonly record struct ZoneEncounter(int Id, string Name);

readonly record struct HeroSpec(string Class, string Name, string Slug);

readonly record struct TakenFight(string Encounter, string Player, string Code, int FightId);

readonly record struct ApiCredentials(string Id, string Secret);

/// <summary>The names, queries and numbers this script uses.</summary>
static class Names
{
    public const string HeroOption = "--hero";
    public const string CountOption = "--count";
    public const string MaxPerPlayerOption = "--max-per-player";
    public const string PagesOption = "--pages";

    public const int DefaultCount = 12;
    public const int DefaultMaxPerPlayer = 2;
    public const int DefaultMaxPages = 10;

    public const string ClientIdVariable = "FELLOWSHIPLOGS_CLIENT_ID";
    public const string ClientSecretVariable = "FELLOWSHIPLOGS_CLIENT_SECRET";

    /// <summary>Folder of the data, beside the solution file.</summary>
    public const string DataFolder = "data";

    /// <summary>Longest wait (minutes) for one FellowshipLogs reply.</summary>
    public const int HttpTimeoutMinutes = 5;

    /// <summary>Seconds added to the wait for the points reset.</summary>
    public const int ResetMarginSeconds = 5;

    public const double SecondsPerMinute = 60;

    /// <summary>Text of the error for a reply that refuses the request for the rate limit.</summary>
    public const string RateLimitedText = "returned 429";

    /// <summary>Matches the text of the error for a server error reply.</summary>
    public const string ServerErrorPattern = @"returned 5\d\d";

    public const string WorldQuery = """
        { worldData { zones { id name encounters { id name } } }
          gameData { classes { slug specs { name slug } } } }
        """;

    public const string RateLimitQuery = "{ rateLimitData { limitPerHour pointsSpentThisHour pointsResetIn } }";

    public const string WorldDataProperty = "worldData";
    public const string ZonesProperty = "zones";
    public const string IdProperty = "id";
    public const string NameProperty = "name";
    public const string EncountersProperty = "encounters";
    public const string GameDataProperty = "gameData";
    public const string ClassesProperty = "classes";
    public const string SpecsProperty = "specs";
    public const string SlugProperty = "slug";
    public const string ReportProperty = "report";
    public const string CodeProperty = "code";
    public const string FightIdProperty = "fightID";
    public const string StartTimeProperty = "startTime";
    public const string RateLimitDataProperty = "rateLimitData";
    public const string LimitPerHourProperty = "limitPerHour";
    public const string PointsSpentThisHourProperty = "pointsSpentThisHour";
    public const string PointsResetInProperty = "pointsResetIn";
    public const string EncounterProperty = "encounter";
    public const string CharacterRankingsProperty = "characterRankings";
    public const string RankingsProperty = "rankings";
    public const string HasMorePagesProperty = "hasMorePages";
}
