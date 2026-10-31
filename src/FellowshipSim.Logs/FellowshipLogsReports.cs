using System.Text.Json.Nodes;

namespace FellowshipSim.Logs;

/// <summary>A report's fights and who played in each, and its combat-log format version (<see cref="GamePatch.LogVersion"/>).</summary>
public sealed record ReportSummary(string Code, string Title, long StartTime, IReadOnlyList<FightSummary> Fights, int? LogVersion = null);

public sealed record FightSummary(int Id, string Name, bool Kill, double DurationSeconds, IReadOnlyList<ReportPlayer> Players);

/// <summary>A player in a report: <see cref="Id"/> is the report's actor ID (the "source" in a FellowshipLogs link).</summary>
public sealed record ReportPlayer(int Id, string Name, string Hero);

/// <summary>Report queries: the fight list of a report, and every event of one fight.</summary>
public static class FellowshipLogsReports
{
    /// <summary>Property names of the raw fight (<see cref="DownloadFightAsync"/>) and of the API response it is built from.</summary>
    private const string ReportDataProperty = "reportData";
    private const string ReportProperty = "report";
    private const string FightsProperty = "fights";
    public const string StartTimeProperty = "startTime";
    public const string EndTimeProperty = "endTime";
    public const string EventsProperty = "events";
    private const string DataProperty = "data";
    public const string TypeProperty = "type";
    public const string SourceIdProperty = "sourceID";
    private const string ApiMasterDataProperty = "masterData";
    public const string ActorsProperty = "actors";
    private const string IdProperty = "id";
    private const string NameProperty = "name";
    public const string SubTypeProperty = "subType";
    public const string FriendlyPlayersProperty = "friendlyPlayers";
    private const string KillProperty = "kill";
    private const string TitleProperty = "title";
    private const string LogVersionProperty = "logVersion";
    private const string NextPageTimestampProperty = "nextPageTimestamp";
    public const string ReportCodeProperty = "report_code";
    public const string ReportStartTimeProperty = "report_start_time";
    public const string FightProperty = "fight";
    public const string MasterDataProperty = "master_data";
    public const string CharacterProperty = "character";

    /// <summary>Type of the event that holds a player's build.</summary>
    public const string CombatantInfoEventType = "combatantinfo";

    /// <summary>Milliseconds in a second: the API times are milliseconds.</summary>
    private const double MillisecondsPerSecond = 1000.0;

    private const string SummaryQuery = """
        query($code: String!) {
          reportData {
            report(code: $code) {
              title startTime
              fights { id name kill startTime endTime friendlyPlayers }
              masterData { logVersion actors(type: "Player") { id name subType } }
            }
          }
        }
        """;

    private const string FightMetaQuery = """
        query($code: String!, $fightIDs: [Int]!) {
          reportData {
            report(code: $code) {
              startTime
              fights(fightIDs: $fightIDs) { id startTime endTime name friendlyPlayers enemyNPCs { id gameID instanceCount } }
              masterData {
                actors { id gameID name type subType petOwner }
                abilities { gameID name type icon }
              }
            }
          }
        }
        """;

    private const string EventsQuery = """
        query($code: String!, $fightIDs: [Int]!, $startTime: Float!, $endTime: Float!) {
          reportData {
            report(code: $code) {
              events(fightIDs: $fightIDs, dataType: All, startTime: $startTime, endTime: $endTime, limit: 10000, includeResources: true) {
                data nextPageTimestamp
              }
            }
          }
        }
        """;

    private const string FightTimesQuery = """
        query($code: String!, $fightIDs: [Int]!) {
          reportData { report(code: $code) { fights(fightIDs: $fightIDs) { startTime endTime } } }
        }
        """;

    private const string CombatantInfoQuery = """
        query($code: String!, $fightIDs: [Int]!, $sourceID: Int!, $startTime: Float!, $endTime: Float!) {
          reportData {
            report(code: $code) {
              events(fightIDs: $fightIDs, dataType: CombatantInfo, sourceID: $sourceID, startTime: $startTime, endTime: $endTime, limit: 10) {
                data
              }
            }
          }
        }
        """;

    /// <summary>
    /// The <c>combatantinfo</c> event of player <paramref name="playerId"/> in one fight (stats, talents and gear), or null
    /// when the fight has none for the player: one small query instead of the fight's every event.
    /// </summary>
    public static async Task<JsonObject?> GetCombatantInfoAsync(this FellowshipLogsClient client, string reportCode, int fightId,
        int playerId, CancellationToken ct = default)
    {
        var fight = (await client.QueryAsync(FightTimesQuery, new FightVariables(reportCode, [fightId]), ct))
            [ReportDataProperty]?[ReportProperty]?[FightsProperty]?.AsArray().FirstOrDefault()
            ?? throw new FellowshipLogsException($"Fight {fightId} is not in report '{reportCode}'.");
        var data = (await client.QueryAsync(CombatantInfoQuery, new CombatantInfoVariables(
            reportCode,
            [fightId],
            playerId,
            fight[StartTimeProperty]!.GetValue<double>(),
            fight[EndTimeProperty]!.GetValue<double>()), ct))[ReportDataProperty]?[ReportProperty]?[EventsProperty]?[DataProperty]?.AsArray();
        return data?.FirstOrDefault(e => e?[TypeProperty]?.GetValue<string>() == CombatantInfoEventType && e[SourceIdProperty]?.GetValue<int>() == playerId)
            ?.DeepClone().AsObject();
    }

    public static async Task<ReportSummary> GetSummaryAsync(this FellowshipLogsClient client, string reportCode, CancellationToken ct = default)
    {
        var data = await client.QueryAsync(SummaryQuery, new ReportVariables(reportCode), ct);
        var report = data[ReportDataProperty]?[ReportProperty] ?? throw new FellowshipLogsException($"Report '{reportCode}' was not found.");
        var players = (report[ApiMasterDataProperty]?[ActorsProperty]?.AsArray() ?? [])
            .Where(a => a != null)
            .ToDictionary(a => a![IdProperty]!.GetValue<int>(), a => new ReportPlayer(
                a![IdProperty]!.GetValue<int>(), a[NameProperty]?.GetValue<string>() ?? "?", a[SubTypeProperty]?.GetValue<string>() ?? ""));
        var fights = new List<FightSummary>();
        foreach (var fight in report[FightsProperty]?.AsArray() ?? [])
        {
            if (fight == null) continue;
            var ids = fight[FriendlyPlayersProperty]?.AsArray().Select(x => x!.GetValue<int>()) ?? [];
            fights.Add(new FightSummary(
                fight[IdProperty]!.GetValue<int>(),
                fight[NameProperty]?.GetValue<string>() ?? "",
                fight[KillProperty]?.GetValue<bool?>() ?? false,
                (fight[EndTimeProperty]!.GetValue<double>() - fight[StartTimeProperty]!.GetValue<double>()) / MillisecondsPerSecond,
                ids.Where(players.ContainsKey).Select(id => players[id]).ToList()));
        }
        return new ReportSummary(reportCode, report[TitleProperty]?.GetValue<string>() ?? reportCode,
            report[StartTimeProperty]?.GetValue<long>() ?? 0, fights, report[ApiMasterDataProperty]?[LogVersionProperty]?.GetValue<int?>());
    }

    /// <summary>
    /// Downloads every event of one fight, with the report's actors and abilities, as a raw fight
    /// (the input of the engine's <c>ReplayScriptBuilder</c>).
    /// </summary>
    public static async Task<JsonObject> DownloadFightAsync(this FellowshipLogsClient client, string reportCode, int fightId,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var meta = await client.QueryAsync(FightMetaQuery, new FightVariables(reportCode, [fightId]), ct);
        var report = meta[ReportDataProperty]?[ReportProperty] ?? throw new FellowshipLogsException($"Report '{reportCode}' was not found.");
        var fight = report[FightsProperty]?.AsArray().FirstOrDefault() ?? throw new FellowshipLogsException($"Fight {fightId} is not in report '{reportCode}'.");
        double start = fight[StartTimeProperty]!.GetValue<double>(), end = fight[EndTimeProperty]!.GetValue<double>();

        var events = new JsonArray();
        double? cursor = start;
        while (cursor is { } from && from < end)
        {
            var page = (await client.QueryAsync(EventsQuery, new FightEventsVariables(reportCode, [fightId], from, end), ct))
                [ReportDataProperty]![ReportProperty]![EventsProperty]!;
            // Moved, not copied: a long fight's events are hundreds of MB as a tree, and the browser app has little room.
            if (page[DataProperty]?.AsArray() is { } data)
            {
                var moved = data.ToArray();
                data.Clear();
                foreach (var logEvent in moved) events.Add(logEvent);
            }
            progress?.Report($"Downloaded {events.Count:N0} events");
            double? next = page[NextPageTimestampProperty]?.GetValue<double?>();
            if (next == null || next <= from) break;
            cursor = next;
        }

        return new JsonObject
        {
            [ReportCodeProperty] = reportCode,
            [ReportStartTimeProperty] = report[StartTimeProperty]?.DeepClone(),
            [FightProperty] = fight.DeepClone(),
            [MasterDataProperty] = report[ApiMasterDataProperty]?.DeepClone(),
            [CharacterProperty] = null,
            [EventsProperty] = events,
        };
    }
}
