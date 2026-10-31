using System.Diagnostics;
using FellowshipSim.Service;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

// Fields of the version the console shows: major, minor and patch.
const int VersionFieldCount = 3;

var app = ServiceApp.Build(args);
if (!ServiceApp.IsLocal(app.Configuration))
{
    app.Run();
    return;
}

// The one-person app: open the browser on it, and run until this console window closes (or Ctrl+C).
await app.StartAsync();
string url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
Console.WriteLine($"SimFell {(ServiceApp.AppVersion is { } version ? $"{version.ToString(VersionFieldCount)} " : "")}is running at {url}");
Console.WriteLine("Close this window to stop it.");
// "--NoBrowser=true" leaves the browser closed.
if (!app.Configuration.GetValue<bool>(ServiceApp.NoBrowserKey))
{
    try
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Open {url} in your browser ({exception.Message}).");
    }
}
await app.WaitForShutdownAsync();
