using FellowshipSim.Logs;
using FellowshipSim.UI.Services;

namespace FellowshipSim.UI.Jobs;

/// <summary>A saved sim or a review in the Results sidebar's recent list.</summary>
public sealed record RecentResult(string Key, string Kind, string Title, string Value, DateTime CreatedAt,
    SavedSim? Sim = null, ReviewSummary? Review = null)
{
    public static RecentResult Of(SavedSim sim) =>
        new($"sim:{sim.Id}", "Sim", sim.Title, $"{sim.Result.MeanDps:N0} DPS", sim.CreatedAt, Sim: sim);

    public static RecentResult Of(ReviewSummary review) =>
        new($"review:{review.Id}", "Replay", $"{review.PlayerName} in {review.FightName}",
            review.LogDamage != 0 ? $"{review.EngineDamage / review.LogDamage:P1} of log" : "", review.CreatedAt, Review: review);
}
