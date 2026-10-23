using System.Text.Json;
using FellowshipSim.Logs;

namespace FellowshipSim.UI.Services;

/// <summary>Finished sims in the data folder's <c>results</c> folder, one JSON file each, so they outlast a restart.</summary>
public sealed class SimResultLibrary(ReplayLibrary library)
{
    public string ResultsDirectory => Path.Combine(library.DataDirectory, "results");

    public void Save(SavedSim sim)
    {
        Directory.CreateDirectory(ResultsDirectory);
        File.WriteAllText(PathOf(sim.Id), JsonSerializer.Serialize(sim, JsonSerializerOptions.Web));
    }

    /// <summary>The saved sim <paramref name="id"/>, or null when there is none.</summary>
    public SavedSim? Get(string id)
    {
        string path = PathOf(id);
        return File.Exists(path) ? Read(path) : null;
    }

    /// <summary>Every saved sim, newest first. A file that does not read is left out.</summary>
    public IReadOnlyList<SavedSim> List() =>
        Directory.Exists(ResultsDirectory)
            ? Directory.GetFiles(ResultsDirectory, "*.json").Select(Read).OfType<SavedSim>().OrderByDescending(s => s.CreatedAt).ToList()
            : [];

    // Only the ID's last path segment names the file, so no ID reaches outside the folder.
    private string PathOf(string id) => Path.Combine(ResultsDirectory, $"{Path.GetFileName(id)}.json");

    private static SavedSim? Read(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<SavedSim>(File.ReadAllText(path), JsonSerializerOptions.Web);
        }
        catch (Exception e) when (e is JsonException or IOException)
        {
            return null;
        }
    }
}
