namespace FellowshipSim.UI.Apl;

/// <summary>The step waiting on the unsaved changes: what it does, for the question, and the step itself.</summary>
public readonly record struct PendingStep(string What, Func<Task> Then);
