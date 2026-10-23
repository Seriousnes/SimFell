using FellowshipSim.Engine;
using FellowshipSim.Logs;
using FellowshipSim.Service.Components;
using FellowshipSim.Service.Services;
using FellowshipSim.UI;
using FellowshipSim.UI.Services;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace FellowshipSim.Service;

/// <summary>
/// FellowshipSim.Service: the FellowshipSim.UI pages (Blazor, Interactive Server), FellowshipLogs access, imports, jobs,
/// saved action lists and the review pages. The engine server (<see cref="RemoteSimEngine"/>) does every replay and sim
/// and reads every action list.
/// </summary>
public static class ServiceApp
{
    /// <summary>The service app, with <paramref name="configure"/> applied to its builder before it is built.</summary>
    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        // The application name picks the assembly whose static assets are served, also when another program hosts this app.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(ServiceApp).Assembly.GetName().Name,
        });
        configure?.Invoke(builder);

        // Downloaded fights, replay scripts, review pages and local settings live in the data folder: "DataDirectory" in
        // configuration, otherwise "data" next to the solution file.
        string dataDirectory = builder.Configuration["DataDirectory"] ?? DefaultDataDirectory(builder.Environment.ContentRootPath);

        builder.AddServiceDefaults();
        // A pasted profile or action list reaches the circuit as one message; SignalR's default limit is 32 KB.
        builder.Services.AddRazorComponents().AddInteractiveServerComponents()
            .AddHubOptions(o => o.MaximumReceiveMessageSize = 1024 * 1024);
        builder.Services.AddHttpClient<ISimEngine, RemoteSimEngine>(c => c.Timeout = Timeout.InfiniteTimeSpan);
        builder.Services.AddSingleton<IReviewPages, ReviewPages>();
        builder.Services.AddSimFellUI(dataDirectory).AddSimFellWebShell();

        var app = builder.Build();

        var library = app.Services.GetRequiredService<ReplayLibrary>();
        app.MapDefaultEndpoints();
        // A review page changes with each review run, so the browser re-checks it each time (no stale page after a rerun).
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(library.ReviewsDirectory),
            RequestPath = "/reviews",
            OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache",
        });
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(Routes).Assembly);

        app.Logger.LogInformation("Data folder: {Data}", library.DataDirectory);
        return app;
    }

    private static string DefaultDataDirectory(string contentRoot)
    {
        for (var dir = new DirectoryInfo(contentRoot); dir != null; dir = dir.Parent)
        {
            if (dir.GetFiles("FellowshipSim.slnx").Length > 0) return Path.Combine(dir.FullName, "data");
        }
        return Path.Combine(contentRoot, "data");
    }
}
