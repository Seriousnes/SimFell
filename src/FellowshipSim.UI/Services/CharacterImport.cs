using FellowshipSim.Core.Import;
using FellowshipSim.Engine.Replay;
using FellowshipSim.Logs;

namespace FellowshipSim.UI.Services;

/// <summary>
/// A player's character as a logged fight shows it: the fight's <c>combatantinfo</c> event read by
/// <see cref="CharacterReader"/>. The fight is downloaded once into the data folder (<see cref="ReplayLibrary"/>), as for a
/// replay.
/// </summary>
public sealed class CharacterImport(ReplayLibrary library, LogsSettingsStore settings)
{
    /// <summary>
    /// The build of player <paramref name="player"/> in fight <paramref name="fightId"/> of report <paramref name="reportCode"/>.
    /// Throws <see cref="FellowshipLogsException"/> when the fight is not downloaded and cannot be, and
    /// <see cref="InvalidOperationException"/> when the fight has no character information for the player.
    /// </summary>
    public async Task<CharacterBuild> ReadAsync(string reportCode, int fightId, FightPlayer player,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var raw = await library.GetRawFightAsync(settings.CreateClient(), reportCode, fightId, progress, ct);
        var combatant = raw["events"]?.AsArray()
            .FirstOrDefault(e => e?["type"]?.GetValue<string>() == "combatantinfo" && e["sourceID"]?.GetValue<int>() == player.Id)
            ?? throw new InvalidOperationException($"Fight {fightId} has no character information for {player.Name}.");
        return CharacterReader.FromCombatantInfo(combatant.AsObject(), player.Name, player.Hero);
    }
}
