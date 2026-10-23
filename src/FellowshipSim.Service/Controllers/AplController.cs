using System.Text.Json;
using FellowshipSim.Core.APL;
using FellowshipSim.Service.Services;
using FellowshipSim.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FellowshipSim.Service.Controllers;

/// <summary>
/// The visual action-list editor: each hero's vocabulary, checking expressions and converting text and actions (all from
/// the engine), and saved lists.
/// </summary>
[ApiController]
[Route("api/apl")]
public sealed class AplController(ActionListStore store, EngineClient engine) : ControllerBase
{
    private static readonly JsonSerializerOptions Web = JsonSerializerOptions.Web;

    /// <summary>The heroes an action list can be written for.</summary>
    [HttpGet("heroes")]
    public async Task<IReadOnlyList<string>> Heroes(CancellationToken ct) => await engine.HeroesAsync(ct);

    /// <summary>The abilities, resources, auras, values and properties an action list for <paramref name="hero"/> can use.</summary>
    [HttpGet("heroes/{hero}/vocabulary")]
    [ProducesResponseType<AplVocabulary>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound, "application/json")]
    public async Task<IActionResult> Vocabulary(string hero, CancellationToken ct) =>
        Pass(await engine.SendAsync(HttpMethod.Get, $"apl/heroes/{Uri.EscapeDataString(hero)}/vocabulary", ct: ct));

    /// <summary>Tokens of an expression with their kinds for <paramref name="hero"/>, and its parse error.</summary>
    [HttpPost("heroes/{hero}/check")]
    [ProducesResponseType<AplExpressionCheck>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound, "application/json")]
    public async Task<IActionResult> Check(string hero, [FromBody] AplText request, CancellationToken ct) =>
        Pass(await engine.SendAsync(HttpMethod.Post, $"apl/heroes/{Uri.EscapeDataString(hero)}/check", request, ct));

    /// <summary>The actions of an action list's text.</summary>
    [HttpPost("parse")]
    [ProducesResponseType<AplDocumentActions>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest, "application/json")]
    public async Task<IActionResult> ParseText([FromBody] AplText request, CancellationToken ct) =>
        Pass(await engine.SendAsync(HttpMethod.Post, "apl/parse", request, ct));

    /// <summary>The text of an action list's actions, or the problem with each field that stops it being written.</summary>
    [HttpPost("write")]
    [ProducesResponseType<AplDocumentText>(StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> Write([FromBody] AplDocumentActions request, CancellationToken ct) =>
        Pass(await engine.SendAsync(HttpMethod.Post, "apl/write", request, ct));

    /// <summary>Every saved action list.</summary>
    [HttpGet("lists")]
    public async Task<IReadOnlyList<ActionListEntry>> Lists(CancellationToken ct) => store.List(await engine.HeroesAsync(ct));

    /// <summary>A saved action list's text and actions.</summary>
    [HttpGet("lists/{hero}/{name}")]
    [ProducesResponseType<SavedActionList>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavedActionList>> Read(string hero, string name, CancellationToken ct)
    {
        if (await HeroNamed(hero, ct) is not { } known) return NotFound(new ApiError($"No hero named {hero}."));
        if (!ActionListStore.IsValidName(name)) return NotFound(new ApiError($"No action list named {name}."));
        string? text = store.Read(known, name);
        if (text == null) return NotFound(new ApiError($"No action list named {name}."));
        var parsed = await engine.SendAsync(HttpMethod.Post, "apl/parse", new AplText(text), ct);
        return parsed.StatusCode == StatusCodes.Status200OK
            ? new SavedActionList(text, JsonSerializer.Deserialize<AplDocumentActions>(parsed.Json, Web)!.Actions, null)
            : new SavedActionList(text, null, JsonSerializer.Deserialize<ApiError>(parsed.Json, Web)!.Error);
    }

    /// <summary>Saves an action list's actions as text, unless a field does not parse.</summary>
    [HttpPut("lists/{hero}/{name}")]
    [ProducesResponseType<ActionListSaved>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ActionListNotSaved>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ActionListSaved>> Save(string hero, string name, [FromBody] AplDocumentActions request, CancellationToken ct)
    {
        if (await HeroNamed(hero, ct) is not { } known) return NotFound(new ApiError($"No hero named {hero}."));
        if (!ActionListStore.IsValidName(name))
        {
            return BadRequest(new ActionListNotSaved("Name the list with letters, digits, spaces, '_' or '-' (up to 64 characters).", []));
        }
        var written = JsonSerializer.Deserialize<AplDocumentText>((await engine.SendAsync(HttpMethod.Post, "apl/write", request, ct)).Json, Web)!;
        if (written.Text is not { } text) return BadRequest(new ActionListNotSaved("Fix the marked fields first.", written.Errors));
        store.Save(known, name, text);
        return new ActionListSaved(text, store.PathOf(known, name));
    }

    /// <summary>The engine's name for <paramref name="hero"/>, or null when the engine has no such hero.</summary>
    private async Task<string?> HeroNamed(string hero, CancellationToken ct) =>
        (await engine.HeroesAsync(ct)).FirstOrDefault(h => h.Equals(hero, StringComparison.OrdinalIgnoreCase));

    private static ContentResult Pass(EngineResponse response) =>
        new() { StatusCode = response.StatusCode, Content = response.Json, ContentType = "application/json" };
}
