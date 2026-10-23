using FellowshipSim.Engine.Jobs;

namespace FellowshipSim.UI.Shell;

/// <summary>
/// A tab of the desktop app. <paramref name="Key"/> is unique among the tabs: the page's type, the job's ID or the
/// review page's file name.
/// </summary>
public sealed record DesktopTab(string Key, DesktopTabKind Kind, string Title, AppPage? Page = null, SimJob? Job = null,
    string? ReviewFile = null)
{
    public bool Closable => Kind != DesktopTabKind.Page;
}
