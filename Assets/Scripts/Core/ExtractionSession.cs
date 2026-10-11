using System;
using System.Collections.Generic;

namespace BottomDrawer.Core
{
    public sealed class ExtractedItem
    {
        public string InstanceId { get; }
        public ItemDefinition Definition { get; }
        public ExtractedItem(string instanceId, ItemDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(instanceId)) throw new ArgumentException("Instance ID required.", nameof(instanceId));
            InstanceId = instanceId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }
    }

    public sealed class ExtractionSession
    {
        private readonly List<ExtractedItem> _loot = new List<ExtractedItem>();
        private readonly HashSet<string> _extractedIds = new HashSet<string>(StringComparer.Ordinal);
        public PlayerProgress Player { get; }
        public SessionStatus Status { get; private set; } = SessionStatus.Ready;
        public float Stamina { get; private set; }
        public IReadOnlyList<ExtractedItem> Loot => _loot;

        public ExtractionSession(PlayerProgress player)
        {
            Player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public void Begin()
        {
            if (Status == SessionStatus.Active) throw new InvalidOperationException("Session is active.");
            if (_loot.Count != 0) throw new InvalidOperationException("Sell banked items before starting another run.");
            _extractedIds.Clear();
            Stamina = GameRules.MaxStamina(Player.Level(UpgradeType.Stamina));
            Status = SessionStatus.Active;
        }

        // Called only while an object is being actively dragged. Frame-rate independent.
        // Returning false means dragging must stop immediately (including on failure).
        public bool DrainForDrag(float weight, float deltaTime)
        {
            if (Status != SessionStatus.Active) return false;
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            float cost = GameRules.DrainPerSecond(weight) * deltaTime;
            if (cost >= Stamina)
            {
                Stamina = 0f;
                Status = SessionStatus.Failed;
                _loot.Clear(); // All unbanked session earnings are forfeited.
                _extractedIds.Clear();
                return false;
            }
            Stamina -= cost;
            return true;
        }

        public bool Extract(string instanceId, ItemDefinition definition)
        {
            if (Status != SessionStatus.Active) return false;
            if (string.IsNullOrWhiteSpace(instanceId)) throw new ArgumentException("Instance ID required.", nameof(instanceId));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!_extractedIds.Add(instanceId)) return false;
            _loot.Add(new ExtractedItem(instanceId, definition));
            return true;
        }

        public bool Bank()
        {
            if (Status != SessionStatus.Active) return false;
            Status = SessionStatus.Banked;
            return true;
        }

        public int Sell(string instanceId)
        {
            if (Status != SessionStatus.Banked) return 0;
            int index = _loot.FindIndex(x => x.InstanceId == instanceId);
            if (index < 0) return 0;
            int price = GameRules.SalePrice(_loot[index].Definition, Player.Level(UpgradeType.Profit));
            Player.Credit(price);
            _loot.RemoveAt(index);
            return price;
        }

        public int SellAll()
        {
            if (Status != SessionStatus.Banked) return 0;
            int earned = 0;
            // No repeated-credit path: sold objects are removed from the list.
            foreach (var item in _loot)
                earned = checked(earned + GameRules.SalePrice(item.Definition, Player.Level(UpgradeType.Profit)));
            Player.Credit(earned);
            _loot.Clear();
            return earned;
        }

        public bool Buy(UpgradeType upgrade)
        {
            if (Status == SessionStatus.Active) return false;
            return Player.Buy(upgrade);
        }

        // Persistence restores banked, previously extracted objects; active runs are
        // deliberately recovered as failed to prevent escaping a risky run by quitting.
        public void Restore(SessionStatus persistedStatus, float persistedStamina, IEnumerable<ExtractedItem> persistedLoot)
        {
            _loot.Clear();
            _extractedIds.Clear();
            if (persistedStatus == SessionStatus.Banked)
            {
                if (persistedLoot == null) throw new ArgumentNullException(nameof(persistedLoot));
                foreach (var item in persistedLoot)
                {
                    if (!_extractedIds.Add(item.InstanceId)) throw new InvalidOperationException("Duplicate saved loot.");
                    _loot.Add(item);
                }
                Status = SessionStatus.Banked;
                Stamina = Math.Max(0, persistedStamina);
            }
            else
            {
                Status = persistedStatus == SessionStatus.Active ? SessionStatus.Failed : persistedStatus;
                Stamina = 0f;
            }
        }
    }
}
