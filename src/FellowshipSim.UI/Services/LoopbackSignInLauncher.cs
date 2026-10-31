using System.Net;
using System.Net.Sockets;
using System.Text;
using FellowshipSim.Logs;

namespace FellowshipSim.UI.Services;

/// <summary>
/// The desktop and local apps' sign-in: the browser opens FellowshipLogs (<paramref name="openBrowser"/>), which sends
/// it back to <see cref="FellowshipLogsSignIn.LoopbackRedirect"/>, where this listens on this computer alone for the
/// code. The page there says the user can close it.
/// </summary>
public sealed class LoopbackSignInLauncher(Func<string, Task> openBrowser) : ISignInLauncher
{
    /// <summary>How long a sign-in waits for the browser to come back.</summary>
    public static readonly TimeSpan Wait = TimeSpan.FromMinutes(5);

    private const string SignInPath = "/signin";

    public async Task<LogsUserToken?> SignInAsync(HttpClient http, string returnTo, CancellationToken ct)
    {
        var verifier = FellowshipLogsSignIn.NewVerifier();
        var state = FellowshipLogsSignIn.NewState();
        var listener = new TcpListener(IPAddress.Loopback, FellowshipLogsSignIn.LoopbackPort);
        try
        {
            listener.Start();
        }
        catch (SocketException)
        {
            throw new FellowshipLogsException($"Another sign-in is already waiting (port {FellowshipLogsSignIn.LoopbackPort} is in use). Finish or close it first.");
        }
        try
        {
            await openBrowser(FellowshipLogsSignIn.AuthorizeUri(FellowshipLogsSignIn.LoopbackRedirect, verifier, state));
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(Wait);
            while (true)
            {
                using var connection = await listener.AcceptTcpClientAsync(timeout.Token);
                var stream = connection.GetStream();
                var target = (await new StreamReader(stream).ReadLineAsync(timeout.Token))?.Split(' ').ElementAtOrDefault(1);
                if (target == null || !target.StartsWith(SignInPath, StringComparison.Ordinal))
                {
                    await RespondAsync(stream, "404 Not Found", "Not here.", timeout.Token);
                    continue;
                }
                var query = LogsSignIn.Query(target.Contains('?') ? target[target.IndexOf('?')..] : "");
                var ours = query.GetValueOrDefault(LogsSignIn.StateParameter) == state;
                var done = ours && query.ContainsKey(LogsSignIn.CodeParameter);
                await RespondAsync(stream, "200 OK", done
                    ? "You are signed in to SimFell. You can close this tab."
                    : "SimFell did not sign you in. You can close this tab and try again.", timeout.Token);
                if (!ours) continue;
                if (!done) throw new FellowshipLogsException(query.GetValueOrDefault(LogsSignIn.ErrorParameter) == LogsSignIn.AccessDenied ? "Signing in was cancelled." : "FellowshipLogs did not sign you in.");
                return await FellowshipLogsSignIn.ExchangeAsync(http, query[LogsSignIn.CodeParameter], verifier, FellowshipLogsSignIn.LoopbackRedirect, ct);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new FellowshipLogsException("The sign-in took too long. Sign in again.");
        }
        finally
        {
            listener.Stop();
        }
    }

    public Task<PendingSignIn?> TakePendingAsync() => Task.FromResult<PendingSignIn?>(null);

    private static async Task RespondAsync(Stream stream, string status, string message, CancellationToken ct)
    {
        var page = $"<!doctype html><meta charset=\"utf-8\"><title>SimFell</title>" +
                      $"<body style=\"font-family:system-ui,sans-serif;background:#000;color:#ecedee;padding:40px\"><p>{WebUtility.HtmlEncode(message)}</p>";
        var body = Encoding.UTF8.GetBytes(page);
        var head = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(head, ct);
        await stream.WriteAsync(body, ct);
    }
}
