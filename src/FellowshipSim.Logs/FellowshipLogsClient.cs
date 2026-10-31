using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FellowshipSim.Logs;

/// <summary>
/// Client for the FellowshipLogs GraphQL API (https://www.fellowshiplogs.com). Authenticates with an API client's ID and
/// secret (client-credentials OAuth) and caches the access token until shortly before it expires; or, made with
/// <see cref="ForUser"/>, as a signed-in user, whose requests spend their own points first.
/// </summary>
public sealed class FellowshipLogsClient
{
    public const string TokenUrl = "https://www.fellowshiplogs.com/oauth/token";
    public const string ApiUrl = "https://www.fellowshiplogs.com/api/v2/client";

    /// <summary>The API for a signed-in user's token.</summary>
    public const string UserApiUrl = "https://www.fellowshiplogs.com/api/v2/user";

    /// <summary>Content type of a GraphQL request.</summary>
    private const string JsonMediaType = "application/json";

    private const string BearerScheme = "Bearer";
    private const string BasicScheme = "Basic";

    /// <summary>User agent of every request.</summary>
    private const string UserAgent = "FellowshipSim/1.0";

    /// <summary>Property of a GraphQL response that holds its errors.</summary>
    private const string ErrorsProperty = "errors";

    /// <summary>Property of a GraphQL response that holds its result.</summary>
    private const string DataProperty = "data";

    /// <summary>Form field of a token request that names the grant.</summary>
    public const string GrantTypeField = "grant_type";

    /// <summary>Grant of an API client's token request.</summary>
    private const string ClientCredentialsGrant = "client_credentials";

    /// <summary>Property of a token response that holds the access token.</summary>
    public const string AccessTokenProperty = "access_token";

    /// <summary>Property of a token response that holds the token's lifetime in seconds.</summary>
    public const string ExpiresInProperty = "expires_in";

    /// <summary>Lifetime (s) assumed for a token whose response gives none.</summary>
    public const double DefaultTokenLifetimeSeconds = 3600;

    /// <summary>A token is renewed this long (s) before it expires, and never kept shorter than this.</summary>
    private const double TokenExpiryMarginSeconds = 60;

    /// <summary>Characters of a response body quoted in an error.</summary>
    private const int ErrorBodyLength = 500;

    private readonly HttpClient _http;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private string? _token;
    private DateTime _tokenExpires;
    private readonly string _apiUrl = ApiUrl;

    public FellowshipLogsClient(HttpClient http, string clientId, string clientSecret)
    {
        _http = http;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    /// <summary>A client that asks as the signed-in user whose token is <paramref name="accessToken"/>.</summary>
    public static FellowshipLogsClient ForUser(HttpClient http, string accessToken) => new(http, accessToken);

    private FellowshipLogsClient(HttpClient http, string accessToken)
    {
        _http = http;
        _clientId = _clientSecret = "";
        _token = accessToken;
        _tokenExpires = DateTime.MaxValue;
        _apiUrl = UserApiUrl;
    }

    /// <summary>Runs a GraphQL query and returns its <c>data</c> object; GraphQL errors are thrown.</summary>
    public async Task<JsonNode> QueryAsync(string query, object? variables = null, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(new GraphQlRequest(query, variables)), Encoding.UTF8, JsonMediaType)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, token);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new FellowshipLogsException($"FellowshipLogs API returned {(int)response.StatusCode}: {Truncate(body)}");
        }
        var json = JsonNode.Parse(body) ?? throw new FellowshipLogsException("FellowshipLogs API returned an empty response.");
        if (json[ErrorsProperty] is JsonArray { Count: > 0 } errors)
        {
            throw new FellowshipLogsException($"FellowshipLogs API error: {Truncate(errors.ToJsonString())}");
        }
        return json[DataProperty] ?? throw new FellowshipLogsException("FellowshipLogs API response has no data.");
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_token != null && DateTime.UtcNow < _tokenExpires) return _token;
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent([new KeyValuePair<string, string>(GrantTypeField, ClientCredentialsGrant)])
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(BasicScheme,
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}")));
        using var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new FellowshipLogsException(
                $"Could not sign in to FellowshipLogs ({(int)response.StatusCode}). Check the API client ID and secret.");
        }
        var json = JsonNode.Parse(body);
        _token = json?[AccessTokenProperty]?.GetValue<string>() ?? throw new FellowshipLogsException("FellowshipLogs returned no access token.");
        var lifetime = json[ExpiresInProperty]?.GetValue<double>() ?? DefaultTokenLifetimeSeconds;
        _tokenExpires = DateTime.UtcNow.AddSeconds(Math.Max(TokenExpiryMarginSeconds, lifetime - TokenExpiryMarginSeconds));
        return _token;
    }

    private static string Truncate(string text) => text.Length > ErrorBodyLength ? text[..ErrorBodyLength] + "..." : text;
}

public sealed class FellowshipLogsException(string message) : Exception(message);
