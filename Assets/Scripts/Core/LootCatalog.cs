using System;
using System.Collections.Generic;

namespace BottomDrawer.Core
{
    public static class LootCatalog
    {
        public static readonly ItemDefinition[] Items =
        {
            new ItemDefinition("pencil", "Old Pencil", 10, Rarity.Common, false),
            new ItemDefinition("coin", "Bronze Coin", 20, Rarity.Common, true),
            new ItemDefinition("button", "Brass Button", 30, Rarity.Common, true),
            new ItemDefinition("key", "Small Key", 40, Rarity.Common, true),
            new ItemDefinition("thimble", "Silver Thimble", 55, Rarity.Uncommon, true),
            new ItemDefinition("envelope", "Sealed Letter", 75, Rarity.Uncommon, false),
            new ItemDefinition("watch", "Pocket Watch", 100, Rarity.Uncommon, true),
            new ItemDefinition("musicbox", "Music Box", 170, Rarity.Rare, true),
            new ItemDefinition("goldwatch", "Golden Pocket Watch", 250, Rarity.Rare, true),
            new ItemDefinition("artifact", "Impossible Relic", 500, Rarity.Anomalous, false)
        };

        public static ItemDefinition Find(string id)
        {
            foreach (var item in Items)
                if (string.Equals(item.Id, id, StringComparison.Ordinal)) return item;
            throw new KeyNotFoundException("Unknown item ID: " + id);
        }
    }

    public static class LootPicker
    {
        // Deterministic by (world seed, slot index, luck level). Existing items keep
        // their generated identities; upgrading Luck never rerolls an existing slot.
        public static ItemDefinition Pick(int worldSeed, int slotIndex, int luckLevel, bool anomaliesUnlocked)
        {
            if (slotIndex < 0) throw new ArgumentOutOfRangeException(nameof(slotIndex));
            GameRules.CheckLevel(luckLevel);
            int mixedSeed = unchecked(worldSeed * 397 ^ (slotIndex + 1) * 7919);
            var rng = new Random(mixedSeed);
            double roll = rng.NextDouble();
            double rare = 0.09 + 0.02 * luckLevel;
            double anomaly = anomaliesUnlocked ? 0.01 : 0.0;
            double uncommon = 0.26;
            Rarity rarity;
            if (roll < anomaly) rarity = Rarity.Anomalous;
            else if (roll < anomaly + rare) rarity = Rarity.Rare;
            else if (roll < anomaly + rare + uncommon) rarity = Rarity.Uncommon;
            else rarity = Rarity.Common;
            var available = new List<ItemDefinition>();
            foreach (var item in LootCatalog.Items)
                if (item.Rarity == rarity) available.Add(item);
            return available[rng.Next(available.Count)];
        }
    }
}
