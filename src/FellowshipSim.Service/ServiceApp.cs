using System.Text.Json.Serialization;
using FellowshipSim.Engine;
using FellowshipSim.Logs;
using FellowshipSim.Service.Services;
using FellowshipSim.UI;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

namespace FellowshipSim.Service;

/// <summary>
/// FellowshipSim.Service: FellowshipLogs access, importing fights, replay progress, saved action lists, and the review pages.
/// The simulation engine (<see cref="EngineClient"/>) does every replay and reads every action list.
/// </summary>
public static class ServiceApp
{
    /// <summary>
    /// The service app, with <paramref name="configure"/> applied to its builder before it is built. The page comes from
    /// <paramref name="page"/>, otherwise from the app's wwwroot folder.
    /// </summary>
    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure = null, IFileProvider? page = null)
    {
        // The application name picks the assembly whose controllers are mapped, also when another program hosts this app.
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
        // A number is read only from a JSON number. Each number is then of type number in FellowshipSim.Web, not number | string.
        builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict);
        builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
        // FellowshipSim.Web's types are generated from the OpenAPI document at build time. The app has no endpoint for it.
        builder.Services.AddOpenApi(o =>
        {
            o.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
            o.AddSchemaTransformer<EveryPropertyWritten>();
            // An operation's ID is its controller and action (Apl_Vocabulary). The client class and method names come from it.
            o.AddOperationTransformer((operation, context, _) =>
            {
                if (context.Description.ActionDescriptor is ControllerActionDescriptor action)
                {
                    operation.OperationId = $"{action.ControllerName}_{action.ActionName}";
                }
                return Task.CompletedTask;
            });
        });
        builder.Services.AddHttpClient<EngineClient>(c => c.Timeout = TimeSpan.FromMinutes(10));
        builder.Services.AddSimFellUI(dataDirectory);
        // Review jobs replay in this process until the engine server's client implements ISimEngine.
        builder.Services.AddSingleton<ISimEngine, LocalSimEngine>();

        var app = builder.Build();

        var library = app.Services.GetRequiredService<ReplayLibrary>();
        page ??= app.Environment.WebRootFileProvider;
        // The pages change with every build and review run, so the browser re-checks them each time (no stale page after an update).
        static void NoCache(StaticFileResponseContext ctx) => ctx.Context.Response.Headers.CacheControl = "no-cache";
        app.MapDefaultEndpoints();
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = page });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = page, OnPrepareResponse = NoCache });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(library.ReviewsDirectory),
            RequestPath = "/reviews",
            OnPrepareResponse = NoCache,
        });
        app.MapControllers();

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
