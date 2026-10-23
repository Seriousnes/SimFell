using FellowshipSim.Engine;
using FellowshipSim.Logs;
using FellowshipSim.Service.Components;
using FellowshipSim.Service.Services;
using FellowshipSim.UI;
using FellowshipSim.UI.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.StaticFiles;

namespace FellowshipSim.Service;

/// <summary>
/// FellowshipSim.Service: the FellowshipSim.UI pages (Blazor, Interactive Server), FellowshipLogs access, imports, jobs,
/// saved action lists and the review pages, each visitor's own (<see cref="VisitorIdentity"/>). The engine server (<see cref="RemoteSimEngine"/>) does every replay and sim
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

        // Each browser is a visitor with its own jobs, sims, reviews and action lists. The cookie keys are in the data
        // folder, so a visitor keeps their ID when the app restarts.
        builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDirectory, "keys")));
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(VisitorIdentity.ConfigureCookie);
        builder.Services.AddScoped(s => VisitorIdentity.Of(s.GetRequiredService<AuthenticationStateProvider>()));

        var app = builder.Build();

        var library = app.Services.GetRequiredService<ReplayLibrary>();
        app.MapDefaultEndpoints();
        app.UseAuthentication();
        app.Use(VisitorIdentity.IssueAsync);
        app.MapGet("/reviews/{file}", ReviewFileAsync);
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies(typeof(Routes).Assembly);

        app.Logger.LogInformation("Data folder: {Data}", library.DataDirectory);
        return app;
    }

    /// <summary>
    /// A file of a review the visitor ran, from the reviews folder; another visitor's review is not found. A review page
    /// changes with each review run, so the browser re-checks it each time.
    /// </summary>
    private static async Task<IResult> ReviewFileAsync(string file, HttpContext context, ReplayLibrary library, VisitorReviews reviews)
    {
        string name = Path.GetFileName(file);
        string reviewId = name.Split('.')[0];
        string path = Path.Combine(library.ReviewsDirectory, name);
        if (VisitorIdentity.IdOf(context.User) is not { } id || !await reviews.HasAsync(new Visitor(id, OwnsDataFolder: false), reviewId, context.RequestAborted)
            || !File.Exists(path))
        {
            return Results.NotFound();
        }
        if (!ContentTypes.TryGetContentType(name, out string? contentType)) contentType = "application/octet-stream";
        context.Response.Headers.CacheControl = "no-cache";
        return Results.File(path, contentType);
    }

    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    private static string DefaultDataDirectory(string contentRoot)
    {
        for (var dir = new DirectoryInfo(contentRoot); dir != null; dir = dir.Parent)
        {
            if (dir.GetFiles("FellowshipSim.slnx").Length > 0) return Path.Combine(dir.FullName, "data");
        }
        return Path.Combine(contentRoot, "data");
    }
}
