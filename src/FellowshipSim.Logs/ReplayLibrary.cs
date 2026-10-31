using System.Text.Json;
using System.Text.Json.Nodes;

namespace FellowshipSim.Logs;

/// <summary>A replay review that has been run: what was replayed and how closely the engine matched the log.</summary>
public sealed record ReviewSummary(
    string Id,
    string ReportCode,
    int FightId,
    string FightName,
    DateTime? FightDate,
    int PlayerId,
    string PlayerName,
    string Hero,
    int PassedSteps,
    int TotalSteps,
    int MatchedHits,
    int LogHits,
    double EngineDamage,
    double LogDamage,
    int ForcedCasts,
    DateTime CreatedAt)
{
    private const string HtmlExtension = ".html";

    public string HtmlFile => $"{Id}{HtmlExtension}";

    /// <summary>The latest free-dice run of the same fight, if any.</summary>
    public FreeDiceSummary? FreeDice { get; init; }
}

/// <summary>A free-dice run of a fight, as shown next to its review (saved as &lt;review&gt;.freedice.json).</summary>
public sealed record FreeDiceSummary(
    int Runs, double LogDamage, double EngineMean, double Engine5, double Engine95, double LogPercentile, DateTime CreatedAt);

/// <summary>
/// A replay review's files: the replay script, the review page, the review as plain text (issues grouped by kind, and the
/// damage calibration per ability), and every engine hit with the log hit it matched, for checking damage formulas in a
/// spreadsheet.
/// </summary>
public sealed record ReviewArtifacts(ReviewSummary Summary, string ScriptJson, string Html, string SummaryText, string HitsCsv);

/// <summary>
/// The local library of fights and replay reviews, kept in a data folder:
/// <c>fights/</c> raw fights downloaded from FellowshipLogs, <c>scripts/</c> replay scripts built from them, and
/// <c>reviews/</c> the review pages with a summary of each.
/// </summary>
public sealed class ReplayLibrary
{
    public const string FightsFolder = "fights";
    public const string ScriptsFolder = "scripts";
    public const string ReviewsFolder = "reviews";

    /// <summary>Between the report code and the fight ID in a file name.</summary>
    private const string FightSeparator = "_fight";

    /// <summary>Parts of a raw fight's file name around the fight separator: the report code and the fight ID.</summary>
    private const int FightNamePartCount = 2;

    /// <summary>Between the fight ID and the player ID in a review ID.</summary>
    private const string PlayerSeparator = "_p";

    private const string RawFightExtension = ".raw.json";
    private const string RawFightPattern = "*" + RawFightExtension;
    private const string ReplayScriptExtension = ".replay.json";
    public const string SummaryExtension = ".summary.json";
    public const string SummaryPattern = "*" + SummaryExtension;
    private const string SummaryTextExtension = ".summary.txt";
    private const string HitsExtension = ".hits.csv";
    private const string FreeDiceExtension = ".freedice.json";

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public ReplayLibrary(string dataDirectory)
    {
        DataDirectory = Path.GetFullPath(dataDirectory);
        Directory.CreateDirectory(FightsDirectory);
        Directory.CreateDirectory(ScriptsDirectory);
        Directory.CreateDirectory(ReviewsDirectory);
    }

    public string DataDirectory { get; }
    public string FightsDirectory => Path.Combine(DataDirectory, FightsFolder);
    public string ScriptsDirectory => Path.Combine(DataDirectory, ScriptsFolder);
    public string ReviewsDirectory => Path.Combine(DataDirectory, ReviewsFolder);

    public static string ReviewId(string reportCode, int fightId, int playerId) => $"{reportCode}{FightSeparator}{fightId}{PlayerSeparator}{playerId}";

    private string RawFightPath(string reportCode, int fightId) => Path.Combine(FightsDirectory, $"{reportCode}{FightSeparator}{fightId}{RawFightExtension}");

    /// <summary>True when the fight's raw events are already downloaded.</summary>
    public bool HasRawFight(string reportCode, int fightId) => File.Exists(RawFightPath(reportCode, fightId));

    /// <summary>The fight's raw events, downloaded from FellowshipLogs the first time and read from disk after that.</summary>
    public async Task<JsonObject> GetRawFightAsync(FellowshipLogsClient? client, string reportCode, int fightId,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var path = await GetRawFightFileAsync(client, reportCode, fightId, progress, ct);
        return JsonNode.Parse(await File.ReadAllTextAsync(path, ct))!.AsObject();
    }

    /// <summary>The path of the fight's raw events, downloaded from FellowshipLogs the first time.</summary>
    public async Task<string> GetRawFightFileAsync(FellowshipLogsClient? client, string reportCode, int fightId,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var path = RawFightPath(reportCode, fightId);
        if (File.Exists(path))
        {
            progress?.Report("Using the downloaded fight");
            return path;
        }
        if (client == null) throw new FellowshipLogsException("Sign in with FellowshipLogs to download fights.");
        progress?.Report("Downloading the fight from FellowshipLogs");
        var raw = await client.DownloadFightAsync(reportCode, fightId, progress, ct);
        await File.WriteAllTextAsync(path, raw.ToJsonString(), ct);
        return path;
    }

    /// <summary>Writes a review's replay script, page, summary, text and hits.</summary>
    public void Save(ReviewArtifacts review)
    {
        var id = review.Summary.Id;
        File.WriteAllText(Path.Combine(ScriptsDirectory, $"{id}{ReplayScriptExtension}"), review.ScriptJson);
        File.WriteAllText(Path.Combine(ReviewsDirectory, review.Summary.HtmlFile), review.Html);
        File.WriteAllText(Path.Combine(ReviewsDirectory, $"{id}{SummaryExtension}"), JsonSerializer.Serialize(review.Summary, Indented));
        File.WriteAllText(Path.Combine(ReviewsDirectory, $"{id}{SummaryTextExtension}"), review.SummaryText);
        File.WriteAllText(Path.Combine(ReviewsDirectory, $"{id}{HitsExtension}"), review.HitsCsv);
    }

    /// <summary>Every review run so far, newest first.</summary>
    public IReadOnlyList<ReviewSummary> ListReviews() =>
        Directory.GetFiles(ReviewsDirectory, SummaryPattern)
            .Select(f => JsonSerializer.Deserialize<ReviewSummary>(File.ReadAllText(f)))
            .Where(s => s != null && File.Exists(Path.Combine(ReviewsDirectory, s.HtmlFile)))
            .Select(s => s! with { FreeDice = LoadFreeDice(s.Id) })
            .OrderByDescending(s => s!.CreatedAt)
            .ToList()!;

    /// <summary>The review <paramref name="id"/>, or null when its summary or page is not in the reviews folder.</summary>
    public ReviewSummary? GetReview(string id)
    {
        var path = Path.Combine(ReviewsDirectory, $"{Path.GetFileName(id)}{SummaryExtension}");
        if (!File.Exists(path) || JsonSerializer.Deserialize<ReviewSummary>(File.ReadAllText(path)) is not { } summary) return null;
        return File.Exists(Path.Combine(ReviewsDirectory, summary.HtmlFile)) ? summary with { FreeDice = LoadFreeDice(summary.Id) } : null;
    }

    private FreeDiceSummary? LoadFreeDice(string id)
    {
        var path = Path.Combine(ReviewsDirectory, $"{id}{FreeDiceExtension}");
        return File.Exists(path) ? JsonSerializer.Deserialize<FreeDiceSummary>(File.ReadAllText(path)) : null;
    }

    /// <summary>Fights already downloaded (report code, fight ID).</summary>
    public IReadOnlyList<DownloadedFight> ListDownloadedFights() =>
        Directory.GetFiles(FightsDirectory, RawFightPattern)
            .Select(f => Path.GetFileName(f).Replace(RawFightExtension, ""))
            .Select(n => n.Split(FightSeparator))
            .Where(p => p.Length == FightNamePartCount && int.TryParse(p[1], out _))
            .Select(p => new DownloadedFight(p[0], int.Parse(p[1])))
            .ToList();
}
