using FellowshipSim.UI.Shared;

namespace FellowshipSim.UI.Pages;

/// <summary>The Sim page's profile text and settings, kept for the circuit or window while another page shows.</summary>
public sealed class SimPageState
{
    public string Text { get; set; } = "";

    public int Iterations { get; set; } = 1000;

    public double DurationSeconds { get; set; } = 300;

    public int Targets { get; set; } = 1;

    public MessageState Status { get; set; } = MessageState.None;
}
