using FellowshipSim.Core.Import;
using FellowshipSim.UI.Build;

namespace FellowshipSim.Tests.GameDataBefore;

/// <summary>The generated gear against the gear of tools/generate-gear.cs, row for row.</summary>
public class GameDataBeforeTests
{
    // Orb of Sanguine Restoration: a Relic item granting relic passive 782, with no dungeon source.
    private const int OrbOfSanguineRestoration = 5919;

    [Fact]
    public void Every_Row_Of_The_Script_Is_A_Generated_Row()
    {
        Assert.Equal(GameGearData.Items.Keys.Order(StringComparer.Ordinal), Core.Import.GameGearData.Items.Keys.Order(StringComparer.Ordinal));
        foreach (var (hero, before) in GameGearData.Items)
        {
            var now = Core.Import.GameGearData.Items[hero].ToDictionary(i => i.Id);
            foreach (var old in before)
            {
                Assert.True(now.TryGetValue(old.Id, out var item), $"{hero}: item {old.Id} ({old.Name})");
                string row = $"{hero}: item {old.Id}";
                Assert.Equal((old.Name, old.Kind, old.ItemLevel, old.MaxTempers, old.Legendary, old.WeaponAbilityId, old.Stats),
                    (item!.Name, item.Kind, item.ItemLevel, item.MaxTempers, item.Legendary, item.WeaponAbilityId, item.Stats));
                Assert.True(old.Dungeons.SequenceEqual(item.Dungeons), row);
                Assert.Equal(GameIcons.Url(old.Icon), item.Icon);
            }
        }
    }

    [Fact]
    public void The_Generated_Rows_Are_In_The_Order_Of_The_Script()
    {
        foreach (var (hero, before) in GameGearData.Items)
        {
            Assert.Equal(before.Select(i => i.Id), Core.Import.GameGearData.Items[hero].Select(i => i.Id).Where(id => id != OrbOfSanguineRestoration));
        }
    }

    [Fact]
    public void The_Generated_Rows_Add_Orb_Of_Sanguine_Restoration_For_Every_Hero()
    {
        var added = Core.Import.GameGearData.Items
            .SelectMany(h => h.Value.Where(i => !GameGearData.Items[h.Key].Any(o => o.Id == i.Id)).Select(i => (Hero: h.Key, Item: i)))
            .ToList();
        Assert.Equal(GameGearData.Items.Keys.Order(StringComparer.Ordinal), added.Select(a => a.Hero).Order(StringComparer.Ordinal));
        Assert.All(added, a =>
        {
            Assert.Equal(OrbOfSanguineRestoration, a.Item.Id);
            Assert.Equal(GearKind.Relic, a.Item.Kind);
            // Game data: item 5919 at item level 315, Heroic, 8 tempers, Expertise 19 untempered.
            Assert.Equal((315, 8, false, 0), (a.Item.ItemLevel, a.Item.MaxTempers, a.Item.Legendary, a.Item.WeaponAbilityId));
            Assert.Equal(new GearStats(0, 0, 0, 19, 0, 0), a.Item.Stats);
            Assert.Empty(a.Item.Dungeons);
        });
    }
}
