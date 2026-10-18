using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

/// <summary>
/// Talent 657: each Freezing Torrent tick has a 7% chance to cast a free Ice Comet on the target (a plain Ice Comet even
/// with Icy Talons, and it can trigger Avalanche, per the user). hero_data: OnTargetPulsatingAoe.FreeCast.ProcChance 0.07.
/// UNVERIFIED: no combat log has this talent.
/// </summary>
public class ColdShower : Talent
{
    public const double ProcChance = 0.07;
    public const string ProcKey = "cold_shower";
    public override string Name => "Cold Shower";
    public override int Id => 657;
    public override void Bind(Hero hero) { }
}
