using FellowshipSim.Core.APL;
using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Import;
using FellowshipSim.Core.Replay;
using Xunit.Abstractions;

namespace FellowshipSim.Tests;

/// <summary>
/// Runs normal (APL) sims with each Rime talent that no recorded combat log uses, checking the talent is imported and
/// its effect shows up: its aura is gained or its damage is dealt. The character is the one from the fight 7 replay.
/// </summary>
public class RimeTalentSmokeTests
{
    private readonly ITestOutputHelper _output;

    public RimeTalentSmokeTests(ITestOutputHelper output) => _output = output;

    private const string Apl = """
        actions+=/flight_of_the_navir
        actions+=/ice_blitz
        actions+=/winters_blessing
        actions+=/talon_strike
        actions+=/rising_talons
        actions+=/ice_comet
        actions+=/glacial_blast
        actions+=/freezing_torrent
        actions+=/bursting_ice
        actions+=/cold_snap
        actions+=/frost_bolt
        """;

    private static readonly string[] BaseTalents = ["Glacial Assault", "Burstbolter", "Cascading Blitz", "Bursting Swallows", "Navir's Keeper", "Wisdom of the North"];

    [Theory]
    [InlineData("Harrowing Ice", "aura:Harrowing Ice")]
    [InlineData("Supreme Torrent", "damage:Freezing Torrent")]
    [InlineData("Coalescing Frost", "damage:Coalescing Frost")]
    [InlineData("Cold Shower", "damage:Ice Comet")]
    [InlineData("Avalanche", "damage:Ice Comet")]
    [InlineData("Icy Flow", "aura:Icy Flow")]
    [InlineData("Frostweaver's Wrath", "aura:Frostweaver's Wrath")]
    [InlineData("Soulfrost Torrent", "aura:Soulfrost Torrent")]
    public async Task Talent_Is_Modeled_And_Takes_Effect(string talent, string evidence)
    {
        foreach (int targets in new[] { 1, 8 })
        {
            var (result, hero, gained) = await Run([.. BaseTalents, talent], targets);
            Assert.DoesNotContain(hero.UnmodeledFeatures, f => f.StartsWith("Talent:"));
            Assert.True(result.MeanDps > 0);
            string[] parts = evidence.Split(':', 2);
            bool seen = parts[0] == "aura"
                ? gained.GetValueOrDefault(parts[1]) > 0
                : result.AbilityDpsBreakdown.GetValueOrDefault(parts[1]) > 0;
            _output.WriteLine($"{talent} on {targets} target(s): {result.MeanDps:N0} DPS, {evidence} -> " +
                              (parts[0] == "aura" ? $"{gained.GetValueOrDefault(parts[1])} gains" : $"{result.AbilityDpsBreakdown.GetValueOrDefault(parts[1]):N0} DPS"));
            Assert.True(seen, $"{talent}: no {evidence} on {targets} target(s)");
        }
    }

    [Fact]
    public async Task Every_Rime_Talent_In_The_Datamine_Is_Modeled()
    {
        await new CodexDataService().WarmupCacheAsync();
        // Talent-slot IDs repeat across heroes, so the slots are read from the datamine file rather than by ID.
        var names = File.ReadLines(GameData.DefaultPath())
            .Where(l => l.Contains("\"talent-slot\"") && l.Contains("\"Rime\""))
            .Select(l => System.Text.Json.JsonDocument.Parse(l).RootElement)
            .Where(s => s.GetProperty("hero").GetString() == "Rime")
            .Select(s => s.GetProperty("talent").GetProperty("name").GetString()!)
            .ToList();
        Assert.Equal(18, names.Count);
        var (_, hero, _) = await Run(names, 1, iterations: 1);
        Assert.DoesNotContain(hero.UnmodeledFeatures, f => f.StartsWith("Talent:"));
    }

    private static async Task<(MonteCarloResult Result, Hero Hero, Dictionary<string, int> Gained)> Run(IEnumerable<string> talents, int targets, int iterations = 10)
    {
        var codex = new CodexDataService();
        await codex.WarmupCacheAsync();
        // The Rime from yMAZVJL9hTpFjqbC fight 7 (sww), as the replay imported it.
        var snapshot = System.Text.Json.JsonSerializer.Deserialize<CharacterSnapshotDto>(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "rime_character.json")))!;
        snapshot.Talents = talents.ToList();
        var hero = FellowshipImporter.ImportRime(snapshot, codex);
        var gained = new Dictionary<string, int>();
        hero.Auras.OnGained += (aura, _, _) => gained[aura.Def.Name] = gained.GetValueOrDefault(aura.Def.Name) + 1;
        var result = new MonteCarloSimulator(hero, AplParser.ParseScript(Apl)).Run(SimTime.FromSeconds(300), iterations, targets);
        return (result, hero, gained);
    }
}
