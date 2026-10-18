using FellowshipSim.Core.Heroes.Rime.Abilities;

namespace FellowshipSim.Core.Heroes.Rime.Talents;

// Talent IDs and values are from the datamine talent records (talent-slot hero "Rime").

/// <summary>Talent 38. Cold Snap grants Glacial Assault stacks; at 4 the next Glacial Blast/Talon Strike is empowered.</summary>
public class GlacialAssault : Talent
{
    public const int TalentId = 38;
    public const string TalentName = "Glacial Assault";
    public const int BuffEffectId = 1377;
    public const int MaxStacks = 4;

    /// <summary>Stacks from Cold Snap; at 4 the next Glacial Blast or Talon Strike is empowered.</summary>
    public static readonly AuraDef Buff = AuraDef.Effect("Glacial Assault", BuffEffectId, maxStacks: MaxStacks);

    public override string Name => TalentName;
    public override int Id => TalentId;
    public override void Bind(Hero hero) { }
}
