// Packages SimFell to share, as the release workflow does, in bin/package:
//
//     SimFell/
//       SimFell.exe       Windows: the launcher (src/FellowshipSim.Launcher)
//       SimFell.command   macOS: a start script to double-click (it opens in Terminal)
//       simfell.sh        Linux: a start script
//       Runtime/          the web app with the engine in-process, self-contained (.NET inside, nothing to install)
//     SimFell-<rid>.zip
//
// Each starts Runtime/FellowshipSim.Service --Local=true: the web app as one person's, in a console window that opens the
// browser; closing it stops SimFell.
//
//     dotnet run tools/package.cs -- [--rid win-x64|osx-arm64|osx-x64|linux-x64] [--version 0.1.0]
//
// The default is win-x64 with no version (a build with no version shows no out-of-date banner). The Windows launcher
// builds on Windows; package macOS and Linux on macOS or Linux, so the zip keeps the files' run permission.
using System.Diagnostics;
using System.IO.Compression;

const string RidOption = "--rid";
const string VersionOption = "--version";
const string DefaultRid = "win-x64";
const string WindowsRidPrefix = "win";
const string MacRidPrefix = "osx";
const string DotnetCommand = "dotnet";
const string Configuration = "Release";
const string BinFolder = "bin";
const string PackageFolder = "package";
const string AppFolder = "SimFell";
const string RuntimeFolder = "Runtime";
const string LauncherFolder = "launcher";
const string ServiceProject = "src/FellowshipSim.Service";
const string LauncherProject = "src/FellowshipSim.Launcher";
const string ServiceExe = "FellowshipSim.Service";
const string LauncherExe = "SimFell.exe";
const string MacScriptName = "SimFell.command";
const string LinuxScriptName = "simfell.sh";
const string SymbolsPattern = "*.pdb";
const string SolutionFile = "FellowshipSim.slnx";

// The assembly metadata the release workflow's version is passed as (ServiceApp.VersionMetadataKey reads it).
const string VersionProperty = "SimFellVersion";

// Owner reads, writes and runs; group and others read and run.
const UnixFileMode ExecutableMode = (UnixFileMode)0b111_101_101;

const int BytesPerMegabyte = 1024 * 1024;

string rid = DefaultRid;
string? version = null;
for (var i= 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case RidOption: rid = args[++i]; break;
        case VersionOption: version = args[++i]; break;
        default: throw new ArgumentException($"Unknown option {args[i]}. Options: --rid <runtime>, --version <major.minor.patch>.");
    }
}
bool windows = rid.StartsWith(WindowsRidPrefix, StringComparison.Ordinal);

string root = FindRoot();
string package = Path.Combine(root, BinFolder, PackageFolder);
string app = Path.Combine(package, AppFolder);
string runtime = Path.Combine(app, RuntimeFolder);
string zip = Path.Combine(package, $"{AppFolder}-{rid}.zip");

if (Directory.Exists(app)) Directory.Delete(app, recursive: true);
Directory.CreateDirectory(app);

var publish = new List<string> { "publish", ServiceProject, "-c", Configuration, "-r", rid, "--self-contained", "true",
    "-p:SatelliteResourceLanguages=en", "-o", runtime };
if (version != null) publish.Add($"-p:{VersionProperty}={version}");
Run([.. publish]);

if (windows)
{
    var launcher = Path.Combine(package, LauncherFolder);
    Run("build", LauncherProject, "-c", Configuration, "-o", launcher);
    File.Copy(Path.Combine(launcher, LauncherExe), Path.Combine(app, LauncherExe));
    Directory.Delete(launcher, recursive: true);
}
else
{
    // The start script runs the app from its own folder; the window it runs in is the console that stops it.
    var script = "#!/bin/sh\n# Starts SimFell: it opens in your browser; close this window to stop it.\n" +
                    "cd \"$(dirname \"$0\")/Runtime\" && exec ./FellowshipSim.Service --Local=true \"$@\"\n";
    var name = rid.StartsWith(MacRidPrefix, StringComparison.Ordinal) ? MacScriptName : LinuxScriptName;
    File.WriteAllText(Path.Combine(app, name), script.Replace("\r\n", "\n"));
    if (!OperatingSystem.IsWindows())
    {
        File.SetUnixFileMode(Path.Combine(app, name), ExecutableMode);
        File.SetUnixFileMode(Path.Combine(runtime, ServiceExe), ExecutableMode);
    }
}

// Only the app: no debug symbols.
foreach (var pdb in Directory.EnumerateFiles(runtime, SymbolsPattern)) File.Delete(pdb);

if (File.Exists(zip)) File.Delete(zip);
ZipFile.CreateFromDirectory(app, zip, CompressionLevel.Optimal, includeBaseDirectory: true);
Console.WriteLine($"{app}{Environment.NewLine}{zip} ({new FileInfo(zip).Length / BytesPerMegabyte} MB)");
return 0;

void Run(params string[] args)
{
    var start = new ProcessStartInfo(DotnetCommand) { WorkingDirectory = root };
    foreach (var arg in args) start.ArgumentList.Add(arg);
    // Every core: MSBuild builds the projects side by side.
    start.ArgumentList.Add($"-maxcpucount:{Environment.ProcessorCount}");
    using var process = Process.Start(start)!;
    process.WaitForExit();
    if (process.ExitCode != 0) throw new InvalidOperationException($"dotnet {string.Join(' ', args)} failed ({process.ExitCode}).");
}

static string FindRoot()
{
    for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, SolutionFile))) return dir.FullName;
    }
    throw new InvalidOperationException("Run this from inside the SimFell checkout.");
}
