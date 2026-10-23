using FellowshipSim.Engine;
using FellowshipSim.UI.Services;

namespace FellowshipSim.UI.Apl;

/// <summary>Saved action lists of the engine's heroes: listing, reading with the engine's parse, and saving the engine's text.</summary>
public sealed class SavedActionLists(ActionListStore store, ISimEngine engine)
{
    /// <summary>Every saved action list.</summary>
    public async Task<IReadOnlyList<ActionListEntry>> ListAsync(CancellationToken ct = default) => store.List(await engine.GetHeroesAsync(ct));

    /// <summary>A saved action list's text and actions. An unknown hero or list throws <see cref="KeyNotFoundException"/>.</summary>
    public async Task<SavedActionList> ReadAsync(string hero, string name, CancellationToken ct = default)
    {
        string known = await HeroNamed(hero, ct) ?? throw new KeyNotFoundException($"No hero named {hero}.");
        string text = (ActionListStore.IsValidName(name) ? store.Read(known, name) : null)
                      ?? throw new KeyNotFoundException($"No action list named {name}.");
        try
        {
            return new SavedActionList(text, (await engine.ParseActionListAsync(text, ct)).Actions, null);
        }
        catch (SimEngineException e)
        {
            return new SavedActionList(text, null, e.Message);
        }
    }

    /// <summary>
    /// Saves the actions as text and returns the file's path. A name that cannot be a file name, or a field that does not
    /// parse, throws <see cref="ActionListNotSavedException"/>; an unknown hero throws <see cref="KeyNotFoundException"/>.
    /// </summary>
    public async Task<string> SaveAsync(string hero, string name, IReadOnlyList<Core.APL.AplDocumentAction> actions, CancellationToken ct = default)
    {
        string known = await HeroNamed(hero, ct) ?? throw new KeyNotFoundException($"No hero named {hero}.");
        if (!ActionListStore.IsValidName(name))
        {
            throw new ActionListNotSavedException("Name the list with letters, digits, spaces, '_' or '-' (up to 64 characters).", []);
        }
        var written = await engine.WriteActionListAsync(actions, ct);
        if (written.Text is not { } text) throw new ActionListNotSavedException("Fix the marked fields first.", written.Errors);
        store.Save(known, name, text);
        return store.PathOf(known, name);
    }

    /// <summary>The engine's name for <paramref name="hero"/>, or null when the engine has no such hero.</summary>
    private async Task<string?> HeroNamed(string hero, CancellationToken ct) =>
        (await engine.GetHeroesAsync(ct)).FirstOrDefault(h => h.Equals(hero, StringComparison.OrdinalIgnoreCase));
}
