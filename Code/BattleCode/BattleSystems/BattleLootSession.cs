using System.Collections.Generic;
using PSB.Code.CoreSystem.Events;
using PSB.Code.CoreSystem.SaveSystem;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Defines;
using YIS.Code.Items;

namespace PSB.Code.BattleCode.BattleSystems
{
    public class BattleLootSession : MonoBehaviour
    {
        public static BattleLootSession Instance { get; private set; }

        private readonly Dictionary<ItemDataSO, int> _counts = new();
        private readonly Dictionary<ItemType, int> _currencyCounts = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            Bus<BattleLootAddedEvent>.OnEvent += OnLootAdded;
            Bus<BattleLootConsumedEvent>.OnEvent += OnLootConsumed;
            Bus<BattleCurrencyAddedEvent>.OnEvent += OnCurrencyAdded;
        }

        private void OnDisable()
        {
            Bus<BattleLootAddedEvent>.OnEvent -= OnLootAdded;
            Bus<BattleLootConsumedEvent>.OnEvent -= OnLootConsumed;
            Bus<BattleCurrencyAddedEvent>.OnEvent -= OnCurrencyAdded;
        }

        private void OnLootAdded(BattleLootAddedEvent evt)
        {
            if (evt.Item == null || evt.Amount <= 0)
                return;

            if (_counts.TryGetValue(evt.Item, out int cur))
                _counts[evt.Item] = cur + evt.Amount;
            else
                _counts.Add(evt.Item, evt.Amount);
        }

        private void OnLootConsumed(BattleLootConsumedEvent evt)
        {
            if (evt.Item == null || evt.Amount <= 0)
                return;

            if (!_counts.TryGetValue(evt.Item, out int cur))
                return;

            cur -= evt.Amount;
            if (cur <= 0)
                _counts.Remove(evt.Item);
            else
                _counts[evt.Item] = cur;
        }
        
        private void OnCurrencyAdded(BattleCurrencyAddedEvent evt)
        {
            if (evt.Amount <= 0)
                return;

            if (_currencyCounts.TryGetValue(evt.Type, out int cur))
                _currencyCounts[evt.Type] = cur + evt.Amount;
            else
                _currencyCounts.Add(evt.Type, evt.Amount);
        }
        
        public Dictionary<ItemDataSO, int> ItemSnapshot()
            => new Dictionary<ItemDataSO, int>(_counts);

        public Dictionary<ItemType, int> CurrencySnapshot()
            => new Dictionary<ItemType, int>(_currencyCounts);

        public void Rollback(InventoryCode targetInventoryCode)
        {
            if (targetInventoryCode == null)
            {
                Debug.LogWarning("[BattleLootSession] targetInventoryCode가 비어 있습니다.");
                return;
            }

            var currencySnapshot = new Dictionary<ItemType, int>(_currencyCounts);
            var itemSnapshot = new Dictionary<ItemDataSO, int>(_counts);

            foreach (var pair in currencySnapshot)
            {
                if (pair.Value > 0)
                    CurrencyContainer.ForceSpend(pair.Key, pair.Value);
            }

            foreach (var pair in itemSnapshot)
            {
                if (pair.Key == null || pair.Value <= 0)
                    continue;

                targetInventoryCode.TryRemoveItem(pair.Key, pair.Value);
            }

            Clear();
        }

        public void Clear()
        {
            _counts.Clear();
            _currencyCounts.Clear();
        }
        
    }
}