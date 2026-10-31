using Microsoft.JSInterop;

namespace FellowshipSim.UI.Services;

/// <summary>Text the browser downloads as a file (download.js): a build's profile, an action list.</summary>
public static class FileDownloads
{
    private const string ModulePath = JsInterop.ContentRoot + "download.js";

    private const string SaveFunction = "save";

    private const string DefaultFileName = "build";

    /// <summary>Saves <paramref name="text"/> as <paramref name="name"/> with <paramref name="extension"/> (".simc").</summary>
    public static async Task SaveAsync(IJSRuntime js, string name, string extension, string text)
    {
        await using var module = await js.InvokeAsync<IJSObjectReference>(JsInterop.ImportFunction, ModulePath);
        await module.InvokeVoidAsync(SaveFunction, FileName(name) + extension, text);
    }

    /// <summary><paramref name="name"/> with every character a file name cannot have as '_'; "build" when empty.</summary>
    public static string FileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(['/', '\\', ':', '*', '?', '"', '<', '>', '|']).ToHashSet();
        string file = new string(name.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim('.', ' ');
        return file.Length > 0 ? file : DefaultFileName;
    }
}
