using System.Security.Cryptography;
using System.Text;
using FellowshipSim.Engine;

namespace FellowshipSim.EngineServer;

/// <summary>
/// The simulation engine service. Its endpoints are private to FellowshipSim.Service: the engine refuses a request without the
/// shared key ("EngineKey" in configuration) as its bearer token.
/// </summary>
public static class EngineApp
{
    /// <summary>The engine app, with <paramref name="configure"/> applied to its builder before it is built.</summary>
    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        // The application name picks the assembly whose controllers are mapped, also when another program hosts this app.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(EngineApp).Assembly.GetName().Name,
        });
        configure?.Invoke(builder);

        string key = builder.Configuration["EngineKey"]
            ?? throw new InvalidOperationException("Set EngineKey: the key FellowshipSim.Service sends with each request.");

        builder.AddServiceDefaults();
        builder.Services.AddControllers();
        builder.Services.AddSingleton<ISimEngine, LocalSimEngine>();
        // A raw fight is the body of a review request, and one fight's events can be over 60 MB.
        builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = null);

        var app = builder.Build();
        app.MapDefaultEndpoints();
        app.Use(async (context, next) =>
        {
            if (!KeyMatches(context.Request.Headers.Authorization, key))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            await next(context);
        });
        app.MapControllers();
        return app;
    }

    private static bool KeyMatches(string? sent, string key) =>
        sent != null && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(sent), Encoding.UTF8.GetBytes($"Bearer {key}"));
}
