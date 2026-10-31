using FellowshipSim.UI.Shell;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Maui;
#if WINDOWS
using Microsoft.Web.WebView2.Core;
#endif

namespace FellowshipSim.Desktop;

/// <summary>
/// The window's page: <see cref="DesktopShell"/> in a BlazorWebView. The web view also serves the reviews folder at the
/// <see cref="ReviewPages"/> addresses.
/// </summary>
public sealed class MainPage : ContentPage
{
    /// <summary>Host page of the web view.</summary>
    private const string HostPagePath = "wwwroot/index.html";

    /// <summary>Element of the host page that holds the app.</summary>
    private const string AppSelector = "#app";

    private readonly ReviewPages _reviews;

    public MainPage(ReviewPages reviews)
    {
        _reviews = reviews;
        var view = new BlazorWebView { HostPage = HostPagePath };
        view.RootComponents.Add(new RootComponent { Selector = AppSelector, ComponentType = typeof(DesktopShell) });
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

    // Review addresses in the web view. On Windows this event sees only the top frame, so an iframe loads without it and a
    // link opens the file itself: the system browser has no route to the virtual host. On Mac Catalyst the event also sees
    // each iframe, and only the web view has the URL scheme, so every review address stays in the web view.
    private void OnUrlLoading(object? sender, UrlLoadingEventArgs urlLoading)
    {
        if (_reviews.File(urlLoading.Url) is not { } path)
            return;
#if MACCATALYST
        urlLoading.UrlLoadingStrategy = UrlLoadingStrategy.OpenInWebView;
#else
        urlLoading.UrlLoadingStrategy = UrlLoadingStrategy.CancelLoad;
        _ = Launcher.OpenAsync(new OpenFileRequest(Path.GetFileName(path), new ReadOnlyFile(path)));
#endif
    }
}
