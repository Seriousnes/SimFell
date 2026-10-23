using FellowshipSim.Engine;

namespace FellowshipSim.UI.Services;

/// <summary>
/// A finished sim as saved in the data folder: its job's ID and title, the profile it ran, its settings and its DPS.
/// </summary>
public sealed record SavedSim(
    string Id,
    string Title,
    string Hero,
    string ProfileName,
    string Profile,
    int Targets,
    SimResult Result,
    DateTime CreatedAt);
