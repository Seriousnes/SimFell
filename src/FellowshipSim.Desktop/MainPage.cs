using FellowshipSim.UI;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Maui;
#if WINDOWS
using Microsoft.Web.WebView2.Core;
#endif

namespace FellowshipSim.Desktop;

/// <summary>
/// The window's page: <see cref="Routes"/> in a BlazorWebView. The web view also serves the reviews folder at the
/// <see cref="ReviewPages"/> addresses.
/// </summary>
public sealed class MainPage : ContentPage
{
    private readonly ReviewPages _reviews;

    public MainPage(ReviewPages reviews)
    {
        _reviews = reviews;
        var view = new BlazorWebView { HostPage = "wwwroot/index.html" };
        view.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = typeof(Routes) });
#if MACCATALYST
        view.BlazorWebViewInitializing += (_, e) =>
            e.Configuration.SetUrlSchemeHandler(new ReviewSchemeHandler(reviews), ReviewPages.Scheme);
#endif
#if WINDOWS
        view.BlazorWebViewInitialized += (_, e) =>
            e.WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(ReviewPages.Host, reviews.Directory,
                CoreWebView2HostResourceAccessKind.Allow);
#endif
        view.UrlLoading += OnUrlLoading;
        Content = view;
    }

    // A review page in an iframe loads in the web view. A link to one leaves the app, and the system browser has no route
    // to the web view's review address, so the link opens the file itself.
    private void OnUrlLoading(object? sender, UrlLoadingEventArgs e)
    {
        if (_reviews.File(e.Url) is not { } path)
            return;
        e.UrlLoadingStrategy = UrlLoadingStrategy.CancelLoad;
        _ = Launcher.OpenAsync(new OpenFileRequest(Path.GetFileName(path), new ReadOnlyFile(path)));
    }
}
