namespace FellowshipSim.UI.Services;

/// <summary>Names the components share when they load a JavaScript module from this library's static assets.</summary>
public static class JsInterop
{
    /// <summary>The JavaScript runtime function that loads a module.</summary>
    public const string ImportFunction = "import";

    /// <summary>The function every module handle exports to release its listeners.</summary>
    public const string DisposeFunction = "dispose";

    /// <summary>The folder of this library's static assets, from which a module path continues.</summary>
    public const string ContentRoot = "./_content/FellowshipSim.UI/";

    /// <summary>The <c>KeyboardEvent.key</c> of the Enter key.</summary>
    public const string EnterKey = "Enter";

    /// <summary>The <c>KeyboardEvent.key</c> of the Escape key.</summary>
    public const string EscapeKey = "Escape";

    /// <summary>The <c>KeyboardEvent.key</c> of the Tab key.</summary>
    public const string TabKey = "Tab";

    /// <summary>The <c>KeyboardEvent.key</c> of the down arrow key.</summary>
    public const string ArrowDownKey = "ArrowDown";

    /// <summary>The <c>KeyboardEvent.key</c> of the up arrow key.</summary>
    public const string ArrowUpKey = "ArrowUp";

    /// <summary>The module of the value and property pickers.</summary>
    public const string ValuePickerModule = ContentRoot + "apl/valuePicker.js";
}
