using System.Security.Cryptography;
using FellowshipSim.EngineServer;
using FellowshipSim.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Photino.NET;

namespace FellowshipSim.Desktop;

/// <summary>
/// FellowshipSim as one program: FellowshipSim.EngineServer and FellowshipSim.Service run in this process on loopback addresses, and the page
/// shows in a window. The engine's key is new each run, and this FellowshipSim.Service is the only holder of it.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        string key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        // The data folder: "DataDirectory" in configuration, otherwise FellowshipSim in the user's local application data.
        string dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FellowshipSim");

        var engine = EngineApp.Build(args, builder =>
        {
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["EngineKey"] = key });
        });
        engine.StartAsync().GetAwaiter().GetResult();

        var page = new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "wwwroot");
        var service = ServiceApp.Build(args, builder =>
        {
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EngineKey"] = key,
                ["services:engine:http:0"] = engine.Urls.First(),
                ["DataDirectory"] = builder.Configuration["DataDirectory"] ?? dataDirectory,
            });
        }, page);
        service.StartAsync().GetAwaiter().GetResult();

        new PhotinoWindow()
            .SetTitle("FellowshipSim")
            .SetUseOsDefaultSize(false)
            .SetSize(1280, 900)
            .Center()
            .Load(service.Urls.First())
            .WaitForClose();

        service.StopAsync().GetAwaiter().GetResult();
        engine.StopAsync().GetAwaiter().GetResult();
    }
}
