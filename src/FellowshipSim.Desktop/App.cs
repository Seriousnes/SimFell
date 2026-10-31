namespace FellowshipSim.Desktop;

public sealed class App(MainPage page) : Application
{
    /// <summary>Width of the window the app opens.</summary>
    private const int WindowWidth = 1280;

    /// <summary>Height of the window the app opens.</summary>
    private const int WindowHeight = 900;

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(page) { Title = "SimFell", Width = WindowWidth, Height = WindowHeight };
}
