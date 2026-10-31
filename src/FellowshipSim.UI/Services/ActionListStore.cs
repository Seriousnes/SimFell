using System.Text.RegularExpressions;
using FellowshipSim.Logs;

namespace FellowshipSim.UI.Services;

/// <summary>
/// A saved action list: its hero, its name, true for a shared list, which a web visitor reads but never changes, and true
/// for a list built into the engine (<see cref="Core.APL.DefaultActionLists"/>), which every visitor reads.
/// </summary>
public sealed record ActionListEntry(string Hero, string Name, bool Shared = false, bool BuiltIn = false);

/// <summary>
/// Saved action lists. A visitor's own lists are their <c>apls/&lt;hero&gt;/&lt;name&gt;.simc</c> in the
/// <see cref="IBlobStore"/>; for the data folder's owner that is the data folder's <c>apls</c> folder. A web visitor also
/// reads the shared lists in that folder.
/// </summary>
public sealed partial class ActionListStore(ReplayLibrary library, IBlobStore store)
{
    /// <summary>The name of the folder (and key prefix) of saved action lists.</summary>
    /// <summary>The first part of a picked list's value for a list built into the engine.</summary>
    public const string BuiltInValue = "builtin";

    /// <summary>The first part of a picked list's value for a shared list.</summary>
    public const string SharedValue = "shared";

    /// <summary>The first part of a picked list's value for one of the visitor's own lists.</summary>
    public const string OwnValue = "own";

    private const string Folder = "apls";

    private const string SearchPattern = "*" + ProfileStore.Extension;

    /// <summary>The data folder's <c>apls</c> folder: the shared lists on the web, and the desktop user's own lists.</summary>
    public string SharedDirectory => Path.Combine(library.DataDirectory, Folder);

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9 _-]{0,63}$")]
    private static partial Regex NamePattern();

    /// <summary>True when <paramref name="name"/> can be a file name: letters, digits, spaces, '_' and '-', up to 64 characters.</summary>
    public static bool IsValidName(string name) => NamePattern().IsMatch(name);

    /// <summary>The visitor's saved lists of <paramref name="heroes"/>, then the shared ones for a web visitor.</summary>
    public async Task<IReadOnlyList<ActionListEntry>> ListAsync(Visitor visitor, IEnumerable<string> heroes, CancellationToken ct = default)
    {
        var entries = new List<ActionListEntry>();
        foreach (var hero in heroes)
        {
            var prefix = visitor.Key($"{Folder}/{hero}");
            entries.AddRange((await store.ListAsync(prefix, ct))
                .Where(key => key.EndsWith(ProfileStore.Extension, StringComparison.Ordinal) && key.LastIndexOf('/') == prefix.Length)
                .Select(key => new ActionListEntry(hero, key[(prefix.Length + 1)..^ProfileStore.Extension.Length])));
            var shared = Path.Combine(SharedDirectory, hero);
            if (!visitor.OwnsDataFolder && Directory.Exists(shared))
            {
                entries.AddRange(Directory.GetFiles(shared, SearchPattern).Select(path => new ActionListEntry(hero, Path.GetFileNameWithoutExtension(path), Shared: true)));
            }
        }
        return entries.OrderBy(e => e.Hero).ThenBy(e => e.Shared).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>The text of a saved list, or null when there is none.</summary>
    public async Task<string?> ReadAsync(Visitor visitor, ActionListEntry entry, CancellationToken ct = default)
    {
        if (!IsValidName(entry.Name)) return null;
        if (!entry.Shared) return await store.ReadAsync(KeyOf(visitor, entry.Hero, entry.Name), ct);
        var path = PathOf(entry.Hero, entry.Name);
        return File.Exists(path) ? await File.ReadAllTextAsync(path, ct) : null;
    }

    /// <summary>Saves <paramref name="text"/> as one of the visitor's own lists.</summary>
    public Task SaveAsync(Visitor visitor, string hero, string name, string text, CancellationToken ct = default) =>
        store.WriteAsync(KeyOf(visitor, hero, name), text, ct);

    /// <summary>Removes one of the visitor's own lists; a shared or built-in list stays.</summary>
    public Task DeleteAsync(Visitor visitor, string hero, string name, CancellationToken ct = default) =>
        IsValidName(name) ? store.DeleteAsync(KeyOf(visitor, hero, name), ct) : Task.CompletedTask;

    /// <summary>Path of the list in <see cref="SharedDirectory"/>.</summary>
    public string PathOf(string hero, string name) => Path.Combine(SharedDirectory, hero, $"{name}{ProfileStore.Extension}");

    private static string KeyOf(Visitor visitor, string hero, string name) => visitor.Key($"{Folder}/{hero}/{name}{ProfileStore.Extension}");
}
