using FellowshipSim.Core.Import;
using FellowshipSim.UI.Build;
using FellowshipSim.UI.Shared;

namespace FellowshipSim.UI.Pages;

/// <summary>The Sim page's tabs.</summary>
public enum SimTab { Import, Hero, Modes, Advanced }

/// <summary>The fights a sim runs: one target, or several at once.</summary>
public enum SimMode { SingleTarget, Aoe }

/// <summary>The Sim page's build, fight settings and messages, kept for the circuit or window while another page shows.</summary>
public sealed class SimPageState
{
    /// <summary>The targets of an AoE sim at first.</summary>
    public const int DefaultAoeTargets = 8;

    public SimTab Tab { get; set; } = SimTab.Hero;

    /// <summary>The build being edited.</summary>
    public BuildEditor Editor { get; set; } = new(BuildEditor.NewBuild("Rime"));

    public CharacterBuild Build => Editor.Build;

    /// <summary>The saved build the editor shows, by name; empty for a build not saved yet.</summary>
    public string SavedName { get; set; } = "";

    public MessageState BuildStatus { get; set; } = MessageState.None;

    /// <summary>A .simc profile pasted on the Import tab.</summary>
    public string ProfileText { get; set; } = "";

    public MessageState ImportStatus { get; set; } = MessageState.None;

    public SimMode Mode { get; set; } = SimMode.SingleTarget;

    public int Iterations { get; set; } = 1000;

    public double DurationSeconds { get; set; } = 300;

    public int AoeTargets { get; set; } = DefaultAoeTargets;

    public int Targets => Mode == SimMode.SingleTarget ? 1 : AoeTargets;

    public MessageState Status { get; set; } = MessageState.None;
}
