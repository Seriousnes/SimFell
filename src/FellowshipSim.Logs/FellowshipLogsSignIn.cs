using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace FellowshipSim.Logs;

/// <summary>
/// Signing a user in to FellowshipLogs: the authorization code flow with PKCE, for SimFell's public API client (no secret:
/// a public client's ID is meant to be seen). The user signs in on FellowshipLogs, which sends them back to one of the
/// client's registered addresses with a code; the code and the verifier its challenge came from give the user's token.
/// Their requests then spend their own points first (FellowshipLogs' user points).
/// </summary>
public static class FellowshipLogsSignIn
{
    /// <summary>SimFell's public FellowshipLogs API client, registered for the three addresses below.</summary>
    public const string ClientId = "01a184e8-f34e-721e-9067-1fe8884bc1ca";

    public const string AuthorizeUrl = "https://www.fellowshiplogs.com/oauth/authorize";

    /// <summary>Where the browser app's sign-in comes back.</summary>
    public const string SiteRedirect = "https://simfelldev.github.io/signin";

    /// <summary>Where the desktop and local apps' sign-in comes back: a listener of theirs on this computer.</summary>
    public const string LoopbackRedirect = "http://127.0.0.1:47213/signin";

    public const int LoopbackPort = 47213;

    /// <summary>Where a local build of the browser app's sign-in comes back.</summary>
    public const string LocalSiteRedirect = "http://127.0.0.1:5000/signin";

    /// <summary>Grant of a token request that exchanges an authorization code.</summary>
    private const string AuthorizationCodeGrant = "authorization_code";

    /// <summary>Grant of a token request that renews an expired token.</summary>
    private const string RefreshTokenGrant = "refresh_token";

    private const string ClientIdField = "client_id";
    private const string CodeField = "code";
    private const string CodeVerifierField = "code_verifier";
    private const string RedirectUriField = "redirect_uri";

    /// <summary>Form field of a refresh request that holds the refresh token, and property of a token response that holds it.</summary>
    private const string RefreshTokenProperty = "refresh_token";

    /// <summary>Random bytes of a PKCE code verifier.</summary>
    private const int VerifierByteCount = 32;

    /// <summary>Random bytes of a state value.</summary>
    private const int StateByteCount = 16;

    /// <summary>A new PKCE code verifier: 32 random bytes, base64url.</summary>
    public static string NewVerifier() => Base64Url(RandomNumberGenerator.GetBytes(VerifierByteCount));

    /// <summary>A new state value, matched on the way back so a sign-in only completes the one this app started.</summary>
    public static string NewState() => Base64Url(RandomNumberGenerator.GetBytes(StateByteCount));

    /// <summary>The S256 challenge of <paramref name="verifier"/>.</summary>
    public static string Challenge(string verifier) => Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    /// <summary>The FellowshipLogs page that signs the user in and sends them back to <paramref name="redirect"/>.</summary>
    public static string AuthorizeUri(string redirect, string verifier, string state) =>
        $"{AuthorizeUrl}?response_type=code&client_id={ClientId}&redirect_uri={Uri.EscapeDataString(redirect)}" +
        $"&code_challenge={Challenge(verifier)}&code_challenge_method=S256&state={Uri.EscapeDataString(state)}";

    /// <summary>The user's token for the code FellowshipLogs sent back to <paramref name="redirect"/>.</summary>
    public static Task<LogsUserToken> ExchangeAsync(HttpClient http, string code, string verifier, string redirect, CancellationToken ct = default) =>
        TokenAsync(http, [
            new(FellowshipLogsClient.GrantTypeField, AuthorizationCodeGrant), new(ClientIdField, ClientId), new(CodeField, code),
            new(CodeVerifierField, verifier), new(RedirectUriField, redirect),
        ], ct);

    /// <summary>A new token for an expired one, from its refresh token.</summary>
    public static Task<LogsUserToken> RefreshAsync(HttpClient http, string refreshToken, CancellationToken ct = default) =>
        TokenAsync(http, [new(FellowshipLogsClient.GrantTypeField, RefreshTokenGrant), new(ClientIdField, ClientId), new(RefreshTokenProperty, refreshToken)], ct);

    private static async Task<LogsUserToken> TokenAsync(HttpClient http, KeyValuePair<string, string>[] form, CancellationToken ct)
    {
        using var response = await http.PostAsync(FellowshipLogsClient.TokenUrl, new FormUrlEncodedContent(form), ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new FellowshipLogsException($"FellowshipLogs did not sign you in ({(int)response.StatusCode}). Try signing in again.");
        }
        var json = JsonNode.Parse(body);
        string token = json?[FellowshipLogsClient.AccessTokenProperty]?.GetValue<string>() ?? throw new FellowshipLogsException("FellowshipLogs returned no access token.");
        double lifetime = json[FellowshipLogsClient.ExpiresInProperty]?.GetValue<double>() ?? FellowshipLogsClient.DefaultTokenLifetimeSeconds;
        return new LogsUserToken(token, json[RefreshTokenProperty]?.GetValue<string>(), DateTime.UtcNow.AddSeconds(lifetime));
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
