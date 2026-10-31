namespace FellowshipSim.Core.Domain;

/// <summary>
/// A talent in a hero's talent tree: its row (1-6), its column (1-3, written a/b/c), its point cost and its talent ID.
/// Players write a pick as row + column letter: "1b + 2a + 2c + 3a + 3b + 4b + 5b + 6c".
/// </summary>
public sealed record TalentSlot(int Row, int Column, int Cost, int TalentId)
{
    /// <summary>The pick as players write it: "1b".</summary>
    public string Code => $"{Row}{(char)('a' + Column - 1)}";

    /// <summary>One row of the tree as it looks in game: the talents in columns a, b and c, all costing <paramref name="cost"/>.</summary>
    public static TalentSlot[] InRow(int row, int cost, int first, int second, int third) =>
        [new(Row: row, Column: 1, Cost: cost, TalentId: first), new(Row: row, Column: 2, Cost: cost, TalentId: second), new(Row: row, Column: 3, Cost: cost, TalentId: third)];

    /// <summary>A whole tree from its rows, top to bottom.</summary>
    public static TalentSlot[] Tree(params TalentSlot[][] rows) => rows.SelectMany(r => r).ToArray();
}
