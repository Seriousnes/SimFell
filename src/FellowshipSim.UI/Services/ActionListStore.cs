using System.Text.RegularExpressions;
using FellowshipSim.Logs;

namespace FellowshipSim.UI.Services;

/// <summary>A saved action list: its hero and its name.</summary>
public sealed record ActionListEntry(string Hero, string Name);

/// <summary>Action lists saved from the page, as <c>apls/&lt;hero&gt;/&lt;name&gt;.simc</c> in the data folder.</summary>
public sealed partial class ActionListStore(ReplayLibrary library)
{
    public string Directory => Path.Combine(library.DataDirectory, "apls");

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9 _-]{0,63}$")]
    private static partial Regex NamePattern();

    /// <summary>True when <paramref name="name"/> can be a file name: letters, digits, spaces, '_' and '-', up to 64 characters.</summary>
    public static bool IsValidName(string name) => NamePattern().IsMatch(name);

    /// <summary>The saved lists of <paramref name="heroes"/>.</summary>
    public IReadOnlyList<ActionListEntry> List(IEnumerable<string> heroes) =>
        heroes
            .Where(hero => System.IO.Directory.Exists(Path.Combine(Directory, hero)))
            .SelectMany(hero => System.IO.Directory.GetFiles(Path.Combine(Directory, hero), "*.simc")
                .Select(path => new ActionListEntry(hero, Path.GetFileNameWithoutExtension(path))))
            .OrderBy(e => e.Hero).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public string? Read(string hero, string name) => File.Exists(PathOf(hero, name)) ? File.ReadAllText(PathOf(hero, name)) : null;

    public void Save(string hero, string name, string text)
    {
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(PathOf(hero, name))!);
        File.WriteAllText(PathOf(hero, name), text);
    }

    public string PathOf(string hero, string name) => Path.Combine(Directory, hero, $"{name}.simc");
}
