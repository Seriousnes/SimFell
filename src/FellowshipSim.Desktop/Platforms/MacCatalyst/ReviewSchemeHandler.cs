using Foundation;
using WebKit;

namespace FellowshipSim.Desktop;

/// <summary>
/// The <see cref="ReviewPages.Scheme"/> URL scheme in the web view: each address answers with its review page from the
/// reviews folder.
/// </summary>
internal sealed class ReviewSchemeHandler(ReviewPages reviews) : NSObject, IWKUrlSchemeHandler
{
    /// <summary>Content type of a review page.</summary>
    private const string HtmlContentType = "text/html; charset=utf-8";

    private const string ContentTypeHeader = "Content-Type";
    private const string HttpVersion = "HTTP/1.1";

    /// <summary>HTTP status of a review page.</summary>
    private const int StatusOk = 200;

    [Export("webView:startURLSchemeTask:")]
    public void StartUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
    {
        var url = urlSchemeTask.Request.Url;
        if (url.AbsoluteString is not { } address || reviews.File(new Uri(address)) is not { } path)
        {
            urlSchemeTask.DidFailWithError(new NSError(NSError.CocoaErrorDomain, (nint)(long)NSCocoaError.FileReadNoSuchFile));
            return;
        }
        using var data = NSData.FromFile(path);
        using var headers = NSDictionary.FromObjectAndKey(new NSString(HtmlContentType), new NSString(ContentTypeHeader));
        using var response = new NSHttpUrlResponse(url, StatusOk, HttpVersion, headers);
        urlSchemeTask.DidReceiveResponse(response);
        urlSchemeTask.DidReceiveData(data);
        urlSchemeTask.DidFinish();
    }

    [Export("webView:stopURLSchemeTask:")]
    public void StopUrlSchemeTask(WKWebView webView, IWKUrlSchemeTask urlSchemeTask)
    {
    }
}
