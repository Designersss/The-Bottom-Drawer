using System;
using System.Collections.Generic;

namespace BottomDrawer.Core
{
    public sealed class PlayerProgress
    {
        private readonly Dictionary<UpgradeType, int> _levels = new Dictionary<UpgradeType, int>();
        public int Coins { get; private set; }

        public PlayerProgress(int coins = 0)
        {
            if (coins < 0) throw new ArgumentOutOfRangeException(nameof(coins));
            Coins = coins;
            foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType))) _levels[type] = 0;
        }

        public int Level(UpgradeType type) => _levels[type];

        public bool Buy(UpgradeType type)
        {
            int level = Level(type);
            if (level >= GameRules.MaxUpgradeLevel) return false;
            int cost = GameRules.UpgradePrice(type, level);
            if (Coins < cost) return false;
            Coins -= cost;
            _levels[type] = level + 1;
            return true;
        }

        public void Credit(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Coins = checked(Coins + value);
        }

        public void RestoreLevel(UpgradeType type, int level)
        {
            GameRules.CheckLevel(level);
            _levels[type] = level;
        }
    }
}
