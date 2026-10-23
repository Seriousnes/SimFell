using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FellowshipSim.Logs;

namespace FellowshipSim.Service.Services;

/// <summary>A request the engine refused or failed, with the engine's message.</summary>
public sealed class EngineException(string message) : Exception(message);

/// <summary>An engine response passed on to the page: its status code and JSON body.</summary>
public sealed record EngineResponse(int StatusCode, string Json);

/// <summary>
/// The simulation engine's private endpoints. The engine's address is the service "engine" (from service discovery,
/// "services:engine:http:0" in configuration). Each request has the shared key ("EngineKey") as its bearer token.
/// </summary>
public sealed class EngineClient
{
    private readonly HttpClient _http;

    public EngineClient(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _http.BaseAddress = new Uri("http://engine");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            configuration["EngineKey"] ?? throw new InvalidOperationException("Set EngineKey: the key the engine expects with each request."));
    }

    /// <summary>The heroes the engine can replay and write action lists for.</summary>
    public async Task<IReadOnlyList<string>> HeroesAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<string>>("heroes", ct) ?? [];

    /// <summary>Replays one player of the raw fight at <paramref name="rawFightPath"/> and returns the review's files.</summary>
    public async Task<ReviewArtifacts> ReviewAsync(string rawFightPath, int playerId, CancellationToken ct = default)
    {
        await using var fight = File.OpenRead(rawFightPath);
        using var content = new StreamContent(fight);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        using var response = await _http.PostAsync($"reviews?playerId={playerId}", content, ct);
        if (!response.IsSuccessStatusCode) throw new EngineException(await ErrorOf(response, ct));
        return await response.Content.ReadFromJsonAsync<ReviewArtifacts>(ct)
            ?? throw new EngineException("The engine returned no review.");
    }

    /// <summary>Sends a request to the engine and returns its response unchanged.</summary>
    public async Task<EngineResponse> SendAsync(HttpMethod method, string path, object? body = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body != null) request.Content = JsonContent.Create(body);
        using var response = await _http.SendAsync(request, ct);
        return new EngineResponse((int)response.StatusCode, await response.Content.ReadAsStringAsync(ct));
    }

    private static async Task<string> ErrorOf(HttpResponseMessage response, CancellationToken ct)
    {
        string text = await response.Content.ReadAsStringAsync(ct);
        try
        {
            if (JsonNode.Parse(text)?["error"]?.GetValue<string>() is { } error) return error;
        }
        catch (System.Text.Json.JsonException)
        {
        }
        return $"The engine failed ({(int)response.StatusCode}).";
    }
}
