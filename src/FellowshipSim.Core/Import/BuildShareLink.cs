using FellowshipSim.Core.Customization.Gems;

namespace FellowshipSim.Core.Import;

/// <summary>
/// A build as a share link: <c>{site}build#{hero}.{code}.{name}</c>, e.g.
/// <c>https://simfelldev.github.io/build#elarion.YSFs…llqw.Iunno</c>. The hero is its name in lower case, the code holds the
/// rest of the build, and the name is the build's, escaped as URL data (optional: a link without one takes the hero's
/// name). The code keeps game IDs, never positions in lists of the engine's, so a link keeps working as content is added.
/// <para>
/// Code version 1 is bits written as URL-safe base 64, 0 bits to the last character. Numbers are Exp-Golomb (EG, of the
/// order given; a list's count is order 0):
/// <list type="bullet">
/// <item>EG0 the version; 3 bits: values in thousandths, stats from an old profile, max health given.</item>
/// <item>The primary stat, stamina, armor and the crit, haste, expertise and spirit ratings, EG7 (EG17 in thousandths);
/// then max health when given, EG14 (EG24).</item>
/// <item>Lists sorted by game ID, each its count, then each item's ID as its gap from the one before (the first's from 0),
/// then what the item holds: talents; augmentations and glyphs, each with its rank less 1 (EG0); gem power by colour,
/// each with its power (EG9, EG19); socketed gems; gear gem bonuses by colour, each with its count (EG0); stat enchants,
/// each with its rating (EG3) and its count (EG0), 0 for none.</item>
/// <item>Relic 1 and relic 2, then (after its count) each legendary, then the weapon ability: 0 for none, else 1 + the
/// zigzag of its ID less <see cref="RelicBase"/>, <see cref="LegendaryBase"/> or <see cref="WeaponBase"/> (EG1;
/// a legendary is never none, so it has no 1 +).</item>
/// </list>
/// Close IDs cost a few bits each, so most of a build is its stats and gem power.
/// </para>
/// </summary>
public static class BuildShareLink
{
    public const int Version = 1;

    /// <summary>What a link has between the site's address and the build.</summary>
    public const string Marker = "build#";

    // The IDs the usual relics, legendaries and weapons are near (Orb of Hidden Riches, Elarion's leggings, Asha's
    // Chronoshift): any ID is written, these only make the usual ones short.
    public const int RelicBase = 5921;
    public const int LegendaryBase = 5898;
    public const int WeaponBase = 1558;

    // Exp-Golomb orders: each fits the size of what it holds.
    private const int StatOrder = 7, HealthOrder = 14, GemPowerOrder = 9, ThousandthsExtraOrder = 10;
    private const int TalentFirstOrder = 7, TalentGapOrder = 2;
    private const int AugmentationFirstOrder = 3, AugmentationGapOrder = 1;
    private const int GlyphFirstOrder = 8, GlyphGapOrder = 1;
    private const int GemFirstOrder = 4, EnchantFirstOrder = 6, EnchantRatingOrder = 3, OffsetOrder = 1;

    /// <summary>Most items a list in a code can have (a longer one is not a build).</summary>
    private const int MaxListItems = 64;

    /// <summary>Separates the hero, the code and the name in a link.</summary>
    private const char PartSeparator = '.';

    /// <summary>Parts of a link without a name: the hero and the code.</summary>
    private const int HeroAndCodeParts = 2;

    /// <summary>Parts of a link with a name: the hero, the code and the name.</summary>
    private const int HeroCodeAndNameParts = 3;

    /// <summary>Thousandths in a unit, for values written in thousandths.</summary>
    private const double ThousandthsPerUnit = 1000.0;

    /// <summary>Bits of the number type the Exp-Golomb length is counted in.</summary>
    private const int NumberBits = 64;

    /// <summary>Bits one character of the URL-safe base 64 alphabet holds.</summary>
    private const int BitsPerCharacter = 6;

    /// <summary>Most 0 bits an Exp-Golomb number starts with (a longer run is not a build).</summary>
    private const int MaxLeadingZeros = 40;

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    /// <summary>The share link of <paramref name="build"/> on the site at <paramref name="siteAddress"/> (ending in '/').</summary>
    public static string Create(string siteAddress, CharacterBuild build)
    {
        string link = $"{siteAddress}{Marker}{build.Hero.ToLowerInvariant()}.{Code(build)}";
        string name = build.Name.Trim();
        if (name.Length == 0 || name.Equals(build.Hero, StringComparison.OrdinalIgnoreCase)) return link;
        string escaped = Uri.EscapeDataString(name);
        // Chat apps leave a final dot out of a link: escape it.
        if (escaped.EndsWith(PartSeparator)) escaped = escaped[..^1] + "%2E";
        return $"{link}.{escaped}";
    }

    /// <summary>
    /// The build of the share link in <paramref name="text"/> (a whole link, or text holding one), or null when it holds
    /// none. Throws <see cref="FormatException"/> for a link that is not a build.
    /// </summary>
    public static CharacterBuild? TryRead(string text)
    {
        int at = text.IndexOf(Marker, StringComparison.OrdinalIgnoreCase);
        if (at < 0) return null;
        string rest = text[(at + Marker.Length)..].Trim();
        int end = rest.IndexOfAny([' ', '\r', '\n', '\t']);
        if (end >= 0) rest = rest[..end];
        var parts = rest.Split(PartSeparator, count: HeroCodeAndNameParts);
        if (parts.Length < HeroAndCodeParts) throw new FormatException("The share link has no build after its hero.");
        string hero = Catalog.Heroes.Keys.FirstOrDefault(h => h.Equals(parts[0], StringComparison.OrdinalIgnoreCase))
                      ?? throw new FormatException($"The share link's hero \"{parts[0]}\" is not one the engine has.");
        var build = FromCode(parts[1]);
        build.Hero = hero;
        build.Name = parts.Length == HeroCodeAndNameParts && parts[2].Length > 0 ? Uri.UnescapeDataString(parts[2]) : hero;
        return build;
    }

    /// <summary>The code of <paramref name="build"/>: everything but its hero and name.</summary>
    public static string Code(CharacterBuild build)
    {
        var writer = new BitWriter();
        writer.EG(Version, 0);
        double[] stats = Stats(build);
        bool thousandths = stats.Append(build.MaxHealth).Concat(build.GemPower.Values).Any(v => v != Math.Floor(v));
        int extra = thousandths ? ThousandthsExtraOrder : 0;
        writer.Bit(thousandths);
        writer.Bit(build.StatsFromOldProfile);
        writer.Bit(build.MaxHealth > 0);
        foreach (double stat in stats) writer.EG(Number(stat, thousandths), StatOrder + extra);
        if (build.MaxHealth > 0) writer.EG(Number(build.MaxHealth, thousandths), HealthOrder + extra);

        WriteSorted(writer, build.Talents, id => id, TalentFirstOrder, TalentGapOrder, _ => { });
        WriteSorted(writer, build.Augmentations, e => e.Id, AugmentationFirstOrder, AugmentationGapOrder, e => writer.EG(Rank(e.Rank), 0));
        WriteSorted(writer, build.Glyphs, e => e.Id, GlyphFirstOrder, GlyphGapOrder, e => writer.EG(Rank(e.Rank), 0));
        WriteSorted(writer, build.GemPower.ToList(), g => (int)g.Key, 0, 0, g => writer.EG(Number(g.Value, thousandths), GemPowerOrder + extra));
        WriteSorted(writer, build.SocketedGemIds, id => id, GemFirstOrder, 0, _ => { });
        WriteSorted(writer, build.GearGemBonuses.ToList(), g => (int)g.Key, 0, 0, g => writer.EG(Whole(g.Value), 0));
        // One list of every stat enchant, with its rating and its count.
        var enchants = build.StatEnchants.Select(e => e.Id).Union(build.StatEnchantCounts.Keys).ToList();
        WriteSorted(writer, enchants, id => id, EnchantFirstOrder, 0, id =>
        {
            writer.EG(Whole(build.StatEnchants.FirstOrDefault(e => e.Id == id)?.Rank ?? 0), EnchantRatingOrder);
            writer.EG(Whole(build.StatEnchantCounts.GetValueOrDefault(id)), 0);
        });

        WriteOptional(writer, build.Relic1ItemId, RelicBase);
        WriteOptional(writer, build.Relic2ItemId, RelicBase);
        writer.EG(build.LegendaryItemIds.Count, 0);
        foreach (int id in build.LegendaryItemIds) writer.EG(Zigzag(id - LegendaryBase), OffsetOrder);
        WriteOptional(writer, build.WeaponAbilityId, WeaponBase);
        return writer.Text();
    }

    /// <summary>The build of <paramref name="code"/>, with no hero or name. Throws <see cref="FormatException"/> for a code that is not one.</summary>
    public static CharacterBuild FromCode(string code)
    {
        var reader = new BitReader(code);
        long version = reader.EG(0);
        if (version != Version) throw new FormatException($"The share link is of version {version}; this SimFell reads version {Version}.");
        bool thousandths = reader.Bit(), old = reader.Bit(), health = reader.Bit();
        int extra = thousandths ? ThousandthsExtraOrder : 0;
        double ReadNumber(int order) => thousandths ? reader.EG(order + extra) / ThousandthsPerUnit : reader.EG(order);
        var build = new CharacterBuild
        {
            StatsFromOldProfile = old,
            PrimaryStat = ReadNumber(StatOrder), Stamina = ReadNumber(StatOrder), Armor = ReadNumber(StatOrder), CritRating = ReadNumber(StatOrder),
            HasteRating = ReadNumber(StatOrder), ExpertiseRating = ReadNumber(StatOrder), SpiritRating = ReadNumber(StatOrder),
        };
        if (health) build.MaxHealth = ReadNumber(HealthOrder);

        ReadSorted(reader, TalentFirstOrder, TalentGapOrder, id => build.Talents.Add(id));
        ReadSorted(reader, AugmentationFirstOrder, AugmentationGapOrder, id => build.Augmentations.Add(new BuildEntry(id, reader.Int(0) + 1)));
        ReadSorted(reader, GlyphFirstOrder, GlyphGapOrder, id => build.Glyphs.Add(new BuildEntry(id, reader.Int(0) + 1)));
        ReadSorted(reader, 0, 0, c => build.GemPower[Color(c)] = ReadNumber(GemPowerOrder));
        ReadSorted(reader, GemFirstOrder, 0, id => build.SocketedGemIds.Add(id));
        ReadSorted(reader, 0, 0, c => build.GearGemBonuses[Color(c)] = reader.Int(0));
        ReadSorted(reader, EnchantFirstOrder, 0, id =>
        {
            int rating = reader.Int(EnchantRatingOrder), count = reader.Int(0);
            if (rating > 0) build.StatEnchants.Add(new BuildEntry(id, rating));
            if (count > 0) build.StatEnchantCounts[id] = count;
        });

        build.Relic1ItemId = ReadOptional(reader, RelicBase);
        build.Relic2ItemId = ReadOptional(reader, RelicBase);
        long legendaries = reader.EG(0);
        if (legendaries > MaxListItems) throw reader.Broken();
        for (int i = 0; i < legendaries; i++) build.LegendaryItemIds.Add(Id(LegendaryBase + Unzigzag(reader.EG(OffsetOrder)), reader));
        build.WeaponAbilityId = ReadOptional(reader, WeaponBase);
        reader.End();
        return build;
    }

    private static double[] Stats(CharacterBuild build) =>
        [build.PrimaryStat, build.Stamina, build.Armor, build.CritRating, build.HasteRating, build.ExpertiseRating, build.SpiritRating];

    // A sorted list: its count, then each item's ID as its gap from the one before (the first's from 0), then what it holds.
    private static void WriteSorted<T>(BitWriter writer, IEnumerable<T> items, Func<T, int> id, int firstOrder, int gapOrder, Action<T> rest)
    {
        var list = items.OrderBy(id).ToList();
        writer.EG(list.Count, 0);
        int previous = 0;
        for (int i = 0; i < list.Count; i++)
        {
            int current = Whole(id(list[i]));
            writer.EG(current - previous, i == 0 ? firstOrder : gapOrder);
            previous = current;
            rest(list[i]);
        }
    }

    private static void ReadSorted(BitReader reader, int firstOrder, int gapOrder, Action<int> item)
    {
        long count = reader.EG(0);
        if (count > MaxListItems) throw reader.Broken();
        long id = 0;
        for (int i = 0; i < count; i++)
        {
            id += reader.EG(i == 0 ? firstOrder : gapOrder);
            item(Id(id, reader));
        }
    }

    // 0 for none, else 1 + the zigzag of the ID less the base.
    private static void WriteOptional(BitWriter writer, int id, int baseId) => writer.EG(id == 0 ? 0 : Zigzag(Whole(id) - baseId) + 1, OffsetOrder);

    private static int ReadOptional(BitReader reader, int baseId)
    {
        long value = reader.EG(OffsetOrder);
        return value == 0 ? 0 : Id(baseId + Unzigzag(value - 1), reader);
    }

    private static int Rank(int rank) => rank >= 1 ? rank - 1 : throw Negative(rank);

    private static int Whole(int value) => value >= 0 ? value : throw Negative(value);

    private static long Number(double value, bool thousandths) =>
        value >= 0 && !double.IsNaN(value) ? (long)Math.Round(thousandths ? value * ThousandthsPerUnit : value) : throw Negative(value);

    private static ArgumentOutOfRangeException Negative(object value) => new(nameof(value), value, "A share link holds no negative values or ranks below 1.");

    private static long Zigzag(long value) => value >= 0 ? 2 * value : -2 * value - 1;

    private static long Unzigzag(long value) => (value & 1) == 0 ? value / 2 : -(value + 1) / 2;

    private static int Id(long id, BitReader reader) => id is >= 0 and <= int.MaxValue ? (int)id : throw reader.Broken();

    private static GemColor Color(int value) => Enum.IsDefined(typeof(GemColor), value) ? (GemColor)value : throw new FormatException("The share link's code is not a build.");

    private sealed class BitWriter
    {
        private readonly List<bool> _bits = [];

        public void Bit(bool bit) => _bits.Add(bit);

        /// <summary>Exp-Golomb of order <paramref name="order"/>: value + 2^order in binary, after as many 0s as it has bits beyond order + 1.</summary>
        public void EG(long value, int order)
        {
            ulong number = (ulong)value + (1UL << order);
            int length = NumberBits - System.Numerics.BitOperations.LeadingZeroCount(number);
            for (int i = 0; i < length - 1 - order; i++) _bits.Add(false);
            for (int i = length - 1; i >= 0; i--) _bits.Add(((number >> i) & 1) == 1);
        }

        public string Text()
        {
            var builder = new System.Text.StringBuilder((_bits.Count + BitsPerCharacter - 1) / BitsPerCharacter);
            for (int i = 0; i < _bits.Count; i += BitsPerCharacter)
            {
                int value = 0;
                for (int k = 0; k < BitsPerCharacter; k++) value = (value << 1) | (i + k < _bits.Count && _bits[i + k] ? 1 : 0);
                builder.Append(Alphabet[value]);
            }
            return builder.ToString();
        }
    }

    private sealed class BitReader
    {
        private readonly List<bool> _bits = [];
        private int _at;

        public BitReader(string code)
        {
            foreach (char character in code)
            {
                int value = Alphabet.IndexOf(character);
                if (value < 0) throw new FormatException($"The share link's code has a character it cannot have ('{character}').");
                for (int k = BitsPerCharacter - 1; k >= 0; k--) _bits.Add(((value >> k) & 1) == 1);
            }
        }

        public bool Bit() => _at < _bits.Count ? _bits[_at++] : throw Broken();

        public long EG(int order)
        {
            int zeros = 0;
            while (!Bit())
            {
                if (++zeros > MaxLeadingZeros) throw Broken();
            }
            ulong number = 1;
            for (int i = 0; i < zeros + order; i++) number = (number << 1) | (Bit() ? 1UL : 0);
            return (long)(number - (1UL << order));
        }

        public int Int(int order)
        {
            long value = EG(order);
            return value <= int.MaxValue ? (int)value : throw Broken();
        }

        /// <summary>Only the 0 bits that fill the last character are left.</summary>
        public void End()
        {
            if (_bits.Count - _at >= BitsPerCharacter || _bits.Skip(_at).Any(b => b)) throw Broken();
        }

        public FormatException Broken() => new("The share link's code is cut short or is not a build.");
    }
}
