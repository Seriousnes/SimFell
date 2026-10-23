using FellowshipSim.Engine;
using FellowshipSim.Logs;
using FellowshipSim.UI.Services;

namespace FellowshipSim.Tests.Services;

/// <summary>Finished sims saved in the data folder: read back by ID, listed newest first, and kept inside the folder.</summary>
public sealed class SimResultLibraryTests : IDisposable
{
    private readonly string _data = Path.Combine(Path.GetTempPath(), $"simfell-results-{Guid.NewGuid():N}");
    private readonly SimResultLibrary _results;

    public SimResultLibraryTests() => _results = new SimResultLibrary(new ReplayLibrary(_data));

    private static SavedSim Sim(string id, DateTime createdAt) => new(id, $"sim {id}", "Rime", "sww", "rime=\"sww\"", 1,
        new SimResult(5000, 4000, 6000, 300, new Dictionary<string, double> { ["Frost Bolt"] = 600 }, 100, 300), createdAt);

    [Fact]
    public void A_Saved_Sim_Reads_Back_By_Its_Id()
    {
        var sim = Sim("a1", new DateTime(2026, 10, 23, 1, 0, 0, DateTimeKind.Utc));
        _results.Save(sim);

        var read = _results.Get("a1");
        Assert.NotNull(read);
        Assert.Equal(sim.Title, read.Title);
        Assert.Equal(5000, read.Result.MeanDps);
        Assert.Equal(600, read.Result.AbilityDps["Frost Bolt"]);
        Assert.Null(_results.Get("missing"));
    }

    [Fact]
    public void Saved_Sims_List_Newest_First_And_Skip_An_Unreadable_File()
    {
        _results.Save(Sim("old", new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc)));
        _results.Save(Sim("new", new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc)));
        File.WriteAllText(Path.Combine(_results.ResultsDirectory, "broken.json"), "{ not json");

        Assert.Equal(["new", "old"], _results.List().Select(s => s.Id));
    }

    [Fact]
    public void An_Id_With_A_Path_Stays_In_The_Results_Folder()
    {
        _results.Save(Sim("..\\..\\escape", DateTime.UtcNow));

        Assert.True(File.Exists(Path.Combine(_results.ResultsDirectory, "escape.json")));
        Assert.False(File.Exists(Path.Combine(_data, "..", "escape.json")));
    }

    [Fact]
    public void No_Results_Folder_Lists_Nothing() => Assert.Empty(_results.List());

    public void Dispose()
    {
        if (Directory.Exists(_data)) Directory.Delete(_data, recursive: true);
    }
}
