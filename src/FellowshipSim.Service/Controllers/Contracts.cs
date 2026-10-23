using System.Text.Json.Serialization;
using FellowshipSim.Core.APL;
using FellowshipSim.Engine.Jobs;
using FellowshipSim.Logs;
using FellowshipSim.UI.Services;

namespace FellowshipSim.Service.Controllers;

/// <summary>Why a request failed.</summary>
public sealed record ApiError(string Error);

/// <summary>
/// Whether FellowshipLogs credentials are set, the heroes the engine can replay, the data folder, and the settings file.
/// </summary>
public sealed record ReplayStatus(bool CredentialsConfigured, IReadOnlyList<string> SupportedHeroes, string DataDirectory, string SettingsPath);

public sealed record CredentialsRequest(string ClientId, string ClientSecret);

public sealed record CredentialsSaved(bool Saved);

/// <summary>
/// A FellowshipLogs report with its fights, and the fight and player its link names (<see cref="SelectedFightId"/>,
/// <see cref="SelectedPlayerId"/>).
/// </summary>
public sealed record Report(
    string Code, string Title, DateTime StartTime, int? SelectedFightId, int? SelectedPlayerId, IReadOnlyList<ReportFight> Fights);

/// <summary>A fight of a report, and whether its raw fight is in the data folder.</summary>
public sealed record ReportFight(int Id, string Name, bool Kill, double DurationSeconds, bool Downloaded, IReadOnlyList<ReportPlayer> Players);

/// <summary>A player of a fight, and whether the engine can replay the player's hero.</summary>
public sealed record ReportPlayer(int Id, string Name, string Hero, bool Supported);

public sealed record ReviewRequest(string ReportCode, int FightId, int PlayerId);

/// <summary>Progress of a <see cref="ReviewJob"/>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReviewJobStatus>))]
public enum ReviewJobStatus
{
    [JsonStringEnumMemberName("running")] Running,
    [JsonStringEnumMemberName("done")] Done,
    [JsonStringEnumMemberName("failed")] Failed,
}

/// <summary>A replay review being made in the background, with its progress.</summary>
public sealed class ReviewJob
{
    public required string Id { get; init; }
    public required string ReportCode { get; init; }
    public required int FightId { get; init; }
    public required int PlayerId { get; init; }
    public ReviewJobStatus Status { get; init; }
    public string Message { get; init; } = "Starting";
    public ReviewSummary? Review { get; init; }
    public string? Error { get; init; }

    /// <summary>The page's view of a review job: a queued job is running, and a cancelled one failed.</summary>
    public static ReviewJob From(SimJob job)
    {
        var target = (ReviewTarget)job.Input!;
        return new ReviewJob
        {
            Id = job.Id,
            ReportCode = target.ReportCode,
            FightId = target.FightId,
            PlayerId = target.PlayerId,
            Status = job.Status switch
            {
                SimJobStatus.Done => ReviewJobStatus.Done,
                SimJobStatus.Failed or SimJobStatus.Cancelled => ReviewJobStatus.Failed,
                _ => ReviewJobStatus.Running,
            },
            Message = job.Status == SimJobStatus.Done ? "Done" : job.Message ?? "Starting",
            Review = job.Result as ReviewSummary,
            Error = job.Status == SimJobStatus.Cancelled ? "Cancelled" : job.Error,
        };
    }
}

/// <summary>A saved action list's text, and its actions, or the parse error when the text does not parse.</summary>
public sealed record SavedActionList(string Text, IReadOnlyList<AplDocumentAction>? Actions, string? Error);

/// <summary>A saved action list's text and its file.</summary>
public sealed record ActionListSaved(string Text, string Path);

/// <summary>Why an action list was not saved, and the problem with each field that does not parse.</summary>
public sealed record ActionListNotSaved(string Error, IReadOnlyList<AplDocumentError> Errors);
