using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace FellowshipSim.UI.Shell;

/// <summary>
/// A page of the app's menu: its title and component. The web app routes to it at the component's <c>@page</c> address;
/// the desktop app shows it as a tab.
/// </summary>
public sealed record AppPage(string Title, Type Component)
{
    public static readonly AppPage Sim = new("Sim", typeof(Pages.Sim));
    public static readonly AppPage Import = new("Import", typeof(Pages.Import));
    public static readonly AppPage Reviews = new("Reviews", typeof(Pages.Reviews));
    public static readonly AppPage ActionLists = new("Action lists", typeof(Pages.ActionLists));
    public static readonly AppPage Setup = new("Setup", typeof(Pages.Setup));

    /// <summary>The menu, in order. The first page opens when the app starts.</summary>
    public static IReadOnlyList<AppPage> All { get; } = [Sim, Import, Reviews, ActionLists, Setup];

    /// <summary>Address of the page relative to the base address, from the component's first <c>@page</c> template.</summary>
    public string Route { get; } = Component.GetCustomAttribute<RouteAttribute>()!.Template.TrimStart('/');
}
