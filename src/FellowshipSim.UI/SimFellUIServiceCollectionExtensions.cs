using FellowshipSim.Engine.Jobs;
using FellowshipSim.Logs;
using FellowshipSim.UI.Apl;
using FellowshipSim.UI.Pages;
using FellowshipSim.UI.Replay;
using FellowshipSim.UI.Services;
using FellowshipSim.UI.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace FellowshipSim.UI;

public static class SimFellUIServiceCollectionExtensions
{
    /// <summary>
    /// The app services the pages inject: the data folder at <paramref name="dataDirectory"/> (<see cref="ReplayLibrary"/>),
    /// <see cref="LogsSettingsStore"/>, <see cref="ActionListStore"/>, the <see cref="SimJobQueue"/> with
    /// <paramref name="jobs"/>, <see cref="ReviewJobs"/>, <see cref="ReportLookup"/>, <see cref="SimResultLibrary"/>, and each page's state for one
    /// circuit or window. The host registers the <see cref="Engine.ISimEngine"/>, the <see cref="IReviewPages"/>, and its
    /// shell with <see cref="AddSimFellWebShell"/> or <see cref="AddSimFellDesktopShell"/>.
    /// </summary>
    public static IServiceCollection AddSimFellUI(this IServiceCollection services, string dataDirectory, SimJobQueueOptions? jobs = null)
    {
        services.AddHttpClient(LogsSettingsStore.HttpClientName, c => c.Timeout = TimeSpan.FromMinutes(2));
        services.AddSingleton(new ReplayLibrary(dataDirectory));
        services.AddSingleton<LogsSettingsStore>();
        services.AddSingleton<ActionListStore>();
        services.AddSingleton(new SimJobQueue(jobs));
        services.AddSingleton<ReviewJobs>();
        services.AddSingleton<ReportLookup>();
        services.AddSingleton<SimResultLibrary>();
        services.AddScoped<FollowedReviews>();
        services.AddScoped<SimPageState>();
        services.AddScoped<ImportPageState>();
        services.AddScoped<ActionListsState>();
        return services;
    }

    /// <summary>The web app's views: <see cref="Routes"/> as the root, each page and result at its own address.</summary>
    public static IServiceCollection AddSimFellWebShell(this IServiceCollection services) =>
        services.AddScoped<IAppViews, WebViews>();

    /// <summary>The desktop app's views: <see cref="DesktopShell"/> as the root, each page and result in a tab.</summary>
    public static IServiceCollection AddSimFellDesktopShell(this IServiceCollection services) =>
        services.AddScoped<DesktopTabs>().AddScoped<IAppViews>(s => s.GetRequiredService<DesktopTabs>());
}
