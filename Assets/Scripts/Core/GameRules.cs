using System;

namespace BottomDrawer.Core
{
    public enum Rarity { Common, Uncommon, Rare, Anomalous }
    public enum UpgradeType { Strength, Stamina, Luck, Profit, Automation }
    public enum SessionStatus { Ready, Active, Banked, Failed }

    public sealed class ItemDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public int BasePrice { get; }
        public Rarity Rarity { get; }
        public bool IsMetal { get; }
        public float Weight => GameRules.WeightFromPrice(BasePrice);

        public ItemDefinition(string id, string name, int basePrice, Rarity rarity, bool isMetal)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An item requires an ID.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("An item requires a name.", nameof(name));
            if (basePrice <= 0) throw new ArgumentOutOfRangeException(nameof(basePrice));
            Id = id;
            Name = name;
            BasePrice = basePrice;
            Rarity = rarity;
            IsMetal = isMetal;
        }
    }

    public static class GameRules
    {
        public const int BaseStamina = 100;
        public const int MaxUpgradeLevel = 5;

        // A strict, globally monotonic invariant: more valuable BASE items always weigh more.
        // Profit upgrades affect sale receipts, never intrinsic item mass.
        public static float WeightFromPrice(int basePrice)
        {
            if (basePrice <= 0) throw new ArgumentOutOfRangeException(nameof(basePrice));
            return 0.25f + 0.015f * basePrice;
        }

        public static float DragSpeed(float weight, int strengthLevel)
        {
            if (weight <= 0f) throw new ArgumentOutOfRangeException(nameof(weight));
            CheckLevel(strengthLevel);
            return 3.2f * (1f + 0.18f * strengthLevel) / (1f + 0.22f * weight);
        }

        public static float DrainPerSecond(float weight)
        {
            if (weight <= 0f) throw new ArgumentOutOfRangeException(nameof(weight));
            // Intentionally independent of Strength. Faster dragging costs less overall time.
            return 1.5f + 1.5f * weight;
        }

        public static int MaxStamina(int staminaLevel)
        {
            CheckLevel(staminaLevel);
            return BaseStamina + 20 * staminaLevel;
        }

        public static int SalePrice(ItemDefinition item, int profitLevel)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            CheckLevel(profitLevel);
            return checked((int)Math.Round(item.BasePrice * (1.0 + 0.05 * profitLevel), MidpointRounding.AwayFromZero));
        }

        public static int UpgradePrice(UpgradeType type, int currentLevel)
        {
            CheckLevel(currentLevel);
            if (currentLevel >= MaxUpgradeLevel) throw new InvalidOperationException("Upgrade is capped.");
            int baseCost = type == UpgradeType.Automation ? 125 : type == UpgradeType.Luck ? 95 : 60;
            return checked(baseCost * (currentLevel + 1) * (currentLevel + 1));
        }

        public static void CheckLevel(int level)
        {
            if (level < 0 || level > MaxUpgradeLevel) throw new ArgumentOutOfRangeException(nameof(level));
        }
    }
}
