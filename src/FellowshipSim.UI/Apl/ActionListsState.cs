using FellowshipSim.Engine;
using FellowshipSim.UI.Services;
using FellowshipSim.UI.Shared;

namespace FellowshipSim.UI.Apl;

/// <summary>
/// The action-list editor's lists and fields, kept for the circuit or window, so unsaved edits last while another page
/// shows.
/// </summary>
public sealed class ActionListsState(ISimEngine engine)
{
    private readonly ISimEngine _engine = engine;

    public ActionListEditor Editor { get; } = new(engine);

    /// <summary>True once the heroes, the first hero's vocabulary and the saved lists have loaded.</summary>
    public bool Loaded { get; set; }

    public IReadOnlyList<string> Heroes { get; set; } = [];

    public IReadOnlyList<ActionListEntry> Saved { get; set; } = [];

    /// <summary>The saved list picked, as hero/name; empty for a new list.</summary>
    public string SelectedSaved { get; set; } = "";

    public string Name { get; set; } = "";

    public string NewList { get; set; } = "";

    /// <summary>The action-list text.</summary>
    public string Text { get; set; } = "";

    /// <summary>True when the text's section is open, as it is when a saved list does not parse.</summary>
    public bool TextOpen { get; set; }

    /// <summary>Key of the text's section: a new key renders it anew with its open attribute.</summary>
    public int TextKey { get; set; }

    public MessageState TextStatus { get; set; } = MessageState.None;

    /// <summary>
    /// Puts <paramref name="text"/> in the editor, as a list of no saved name. Before the editor has loaded, the text waits
    /// for it.
    /// </summary>
    public async Task LoadTextAsync(string text, CancellationToken ct = default)
    {
        Text = text;
        SelectedSaved = "";
        Name = "";
        if (!Loaded || Editor.Vocabulary is not { } v) return;
        try
        {
            Editor.Load(v, (await _engine.ParseActionListAsync(text, ct)).Actions);
            TextStatus = MessageState.None;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            TextStatus = MessageState.Error(e.Message);
            TextOpen = true;
            TextKey++;
        }
    }
}
