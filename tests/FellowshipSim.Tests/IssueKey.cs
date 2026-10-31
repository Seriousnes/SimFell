using FellowshipSim.Engine.Replay;

namespace FellowshipSim.Tests;

/// <summary>The category and the subject that group a replay's issues.</summary>
public readonly record struct IssueKey(IssueCategory Category, string? Subject);
