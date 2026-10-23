using System.Text.Json;
using FellowshipSim.Logs;

namespace FellowshipSim.UI.Services;

/// <summary>The FellowshipLogs API client credentials.</summary>
public sealed record LogsCredentials(string ClientId, string ClientSecret);

/// <summary>
/// Where the FellowshipLogs API credentials come from: the environment variables FELLOWSHIPLOGS_CLIENT_ID and
/// FELLOWSHIPLOGS_CLIENT_SECRET if set, otherwise <c>settings.json</c> in the data folder (saved from the web page;
/// the data folder is never committed).
/// </summary>
public sealed class LogsSettingsStore(ReplayLibrary library, IHttpClientFactory httpClients)
{
    /// <summary>Name of the <see cref="HttpClient"/> of FellowshipLogs requests.</summary>
    public const string HttpClientName = "fellowshiplogs";

    public string SettingsPath => Path.Combine(library.DataDirectory, "settings.json");

    public LogsCredentials? Credentials
    {
        get
        {
            string? id = Environment.GetEnvironmentVariable("FELLOWSHIPLOGS_CLIENT_ID");
            string? secret = Environment.GetEnvironmentVariable("FELLOWSHIPLOGS_CLIENT_SECRET");
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(secret)) return new LogsCredentials(id, secret);
            if (!File.Exists(SettingsPath)) return null;
            var saved = JsonSerializer.Deserialize<LogsCredentials>(File.ReadAllText(SettingsPath));
            return saved is { ClientId.Length: > 0, ClientSecret.Length: > 0 } ? saved : null;
        }
    }

    public void Save(LogsCredentials credentials) =>
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true }));

    /// <summary>
    /// Saves the credentials after checking they can sign in. Throws <see cref="ArgumentException"/> when either is blank,
    /// and <see cref="FellowshipLogsException"/> when FellowshipLogs refuses them.
    /// </summary>
    public async Task SignInAndSaveAsync(string clientId, string clientSecret, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new ArgumentException("Enter both the client ID and the client secret.");
        }
        var credentials = new LogsCredentials(clientId.Trim(), clientSecret.Trim());
        await Create(credentials).QueryAsync("query { __typename }", ct: ct);
        Save(credentials);
    }

    /// <summary>A client signed in with the stored credentials, or null when none are set.</summary>
    public FellowshipLogsClient? CreateClient() =>
        Credentials is { } c ? Create(c) : null;

    public FellowshipLogsClient Create(LogsCredentials c) => new(httpClients.CreateClient(HttpClientName), c.ClientId, c.ClientSecret);
}
