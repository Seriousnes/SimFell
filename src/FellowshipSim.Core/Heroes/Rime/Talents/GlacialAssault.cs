using FellowshipSim.Core.Codex;
using FellowshipSim.Core.Domain;
using FellowshipSim.Core.Engine;
using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

// Talent IDs and values are from the datamine talent records (talent-slot hero "Rime").

/// <summary>Talent 38. Cold Snap grants Glacial Assault stacks; at 4 the next Glacial Blast/Talon Strike is empowered.</summary>
public class GlacialAssault : Talent
{
    public override string Name => "Glacial Assault";
    public override int Id => 38;
    public override void Bind(Hero hero) { }
}
