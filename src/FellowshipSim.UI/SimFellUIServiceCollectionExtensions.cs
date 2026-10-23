using FellowshipSim.Engine.Jobs;
using FellowshipSim.Logs;
using FellowshipSim.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FellowshipSim.UI;

public static class SimFellUIServiceCollectionExtensions
{
    /// <summary>
    /// The app services the pages inject: the data folder at <paramref name="dataDirectory"/> (<see cref="ReplayLibrary"/>),
    /// <see cref="LogsSettingsStore"/>, <see cref="ActionListStore"/>, the <see cref="SimJobQueue"/> with
    /// <paramref name="jobs"/>, and <see cref="ReviewJobs"/>. The host registers the <see cref="Engine.ISimEngine"/>.
    /// </summary>
    public static IServiceCollection AddSimFellUI(this IServiceCollection services, string dataDirectory, SimJobQueueOptions? jobs = null)
    {
        services.AddHttpClient(LogsSettingsStore.HttpClientName, c => c.Timeout = TimeSpan.FromMinutes(2));
        services.AddSingleton(new ReplayLibrary(dataDirectory));
        services.AddSingleton<LogsSettingsStore>();
        services.AddSingleton<ActionListStore>();
        services.AddSingleton(new SimJobQueue(jobs));
        services.AddSingleton<ReviewJobs>();
        return services;
    }
}
