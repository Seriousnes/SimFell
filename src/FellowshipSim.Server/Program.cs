using FellowshipSim.Core.Codex;
using FellowshipSim.Logs;
using FellowshipSim.Server.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Downloaded fights, replay scripts, review pages and local settings live in the data folder: "DataDirectory" in
// configuration, otherwise "data" next to the solution file.
string dataDirectory = builder.Configuration["DataDirectory"] ?? DefaultDataDirectory(builder.Environment.ContentRootPath);

builder.Services.AddControllers();
builder.Services.AddHttpClient("fellowshiplogs", c => c.Timeout = TimeSpan.FromMinutes(2));
builder.Services.AddSingleton<ICodexDataService, CodexDataService>();
builder.Services.AddSingleton(sp => new ReplayLibrary(dataDirectory, sp.GetRequiredService<ICodexDataService>()));
builder.Services.AddSingleton<LogsSettingsStore>();
builder.Services.AddSingleton<ReviewJobs>();

var app = builder.Build();

// The game data (Codex datamine) is downloaded once and cached in ~/.fellowsim/cache/codex.
try
{
    await app.Services.GetRequiredService<ICodexDataService>().WarmupCacheAsync();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Could not load the game data yet; it will be retried when a replay runs.");
}

var library = app.Services.GetRequiredService<ReplayLibrary>();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(library.ReviewsDirectory),
    RequestPath = "/reviews",
});
app.MapControllers();

app.Logger.LogInformation("Data folder: {Data}", library.DataDirectory);
app.Run();

static string DefaultDataDirectory(string contentRoot)
{
    for (var dir = new DirectoryInfo(contentRoot); dir != null; dir = dir.Parent)
    {
        if (dir.GetFiles("FellowshipSim.slnx").Length > 0) return Path.Combine(dir.FullName, "data");
    }
    return Path.Combine(contentRoot, "data");
}
