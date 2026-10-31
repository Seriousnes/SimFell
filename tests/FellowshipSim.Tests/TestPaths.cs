namespace FellowshipSim.Tests;

internal static class TestPaths
{
    private const string SolutionFileName = "FellowshipSim.slnx";

    /// <summary>The data folder under the repository root: fights, scripts, reviews, results and accuracy files.</summary>
    public const string DataFolder = "data";

    /// <summary>The source folder under the repository root.</summary>
    public const string SourceFolder = "src";

    /// <summary>The FellowshipSim.UI project folder under the source folder.</summary>
    public const string UiProjectFolder = "FellowshipSim.UI";

    /// <summary>The folder of the test fixtures, copied beside the test assembly.</summary>
    public const string FixturesFolder = "Fixtures";

    /// <summary>The Rime character fixture in <see cref="FixturesFolder"/>.</summary>
    public const string RimeCharacterFile = "rime_character.json";

    /// <summary>The folder holding FellowshipSim.slnx.</summary>
    public static string RepositoryRoot { get; } = Find();

    private static string Find()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (dir.GetFiles(SolutionFileName).Length > 0) return dir.FullName;
        }
        throw new DirectoryNotFoundException("Could not find the repository root (FellowshipSim.slnx).");
    }
}
