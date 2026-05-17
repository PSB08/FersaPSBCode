using System;
using System.Collections.Generic;
using PSB.Code.BattleCode.Events;
using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.InputSystem;
using YIS.Code.Defines;

namespace PSB.Code.CoreSystem.SaveSystem
{
    [DefaultExecutionOrder(-20)]
    public class CurrencyContainer : MonoBehaviour, ISaveable
    {
        public static CurrencyContainer Instance { get; private set; }
        [field: SerializeField] public SaveId SaveId { get; private set; }

        [SerializeField] private bool clampToZero = true;
        [SerializeField] private bool raiseSaveEvt = true;
        
        public const int MAX_COIN = 5000;

        [Serializable]
        public struct CurrencyInit
        {
            public ItemType type;
            public int value;
        }

        [Serializable]
        private struct CurrencyEntry
        {
            public ItemType type;
            public int value;
        }

        [Serializable]
        private struct SaveCollection
        {
            public List<CurrencyEntry> entries;
        }

        [SerializeField] private List<CurrencyInit> defaultCurrencies = new List<CurrencyInit>()
        {
            new CurrencyInit {type = ItemType.Coin, value = 0 },
            new CurrencyInit {type = ItemType.BossCoin, value = 0 },
            new CurrencyInit {type = ItemType.PP, value = 0 },
        };

        private readonly Dictionary<ItemType, int> _map = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureDefaults();
        }

        //#if UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current.f1Key.wasPressedThisFrame)
                Add(ItemType.Coin, 1000);
            if (Keyboard.current.f2Key.wasPressedThisFrame)
                Add(ItemType.PP, 1000);
            if (Keyboard.current.f3Key.wasPressedThisFrame)
                Add(ItemType.BossCoin, 1000);
        }
        //#endif
        
        private void OnEnable()
        {
            Bus<VillageResetEvent>.OnEvent += HandleVillageReset;
        }

        private void HandleVillageReset(VillageResetEvent evt)
        {
            int currentCoin = Get(ItemType.Coin);
            if (currentCoin > MAX_COIN)
            {
                _map[ItemType.Coin] = MAX_COIN;
                Bus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(ItemType.Coin, currentCoin, MAX_COIN));
            }

            int currentPp = Get(ItemType.PP);
            if (currentPp > 0)
            {
                _map[ItemType.PP] = 0;
                Bus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(ItemType.PP, currentPp, 0));
            }

            if (raiseSaveEvt)
                Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
                
            Debug.Log("<color=purple>마을 초기화 - 재화</color>");
        }

        private void OnDisable()
        {
            Bus<VillageResetEvent>.OnEvent -= HandleVillageReset;
        }

        private void EnsureDefaults()
        {
            foreach (var d in defaultCurrencies)
            {
                if (!_map.ContainsKey(d.type))
                    _map[d.type] = d.value;
            }
        }

        public static int Get(ItemType type)
        {
            EnsureInstance();
            return Instance._map.TryGetValue(type, out var v) ? v : 0;
        }

        public static bool Has(ItemType type, int amount)
        {
            if (amount <= 0) return true;
            return Get(type) >= amount;
        }

        public static void Add(ItemType type, int amount)
        {
            if (amount <= 0) return;
            EnsureInstance();
            Instance.Mutate(type, +amount);
        }

        public static bool Spend(ItemType type, int amount)
        {
            if (amount <= 0) return true;
            EnsureInstance();

            if (Get(type) < amount)
                return false;
            
            Instance.Mutate(type, -amount);
            return true;
        }

        public static void ForceSpend(ItemType type, int amount)
        {
            if (amount <= 0) return;
            EnsureInstance();
            Instance.Mutate(type, -amount);
        }

        private void Mutate(ItemType type, int amount)
        {
            _map.TryGetValue(type, out int oldValue);
            int newValue = oldValue + amount;
            
            if (clampToZero && newValue < 0)
                newValue = 0;
            
            _map[type] = newValue;
            
            Bus<CurrencyChangedEvent>.Raise(new CurrencyChangedEvent(type, oldValue, newValue));
            
            if (raiseSaveEvt)
                Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
        }

        private static void EnsureInstance()
        {
            if (Instance != null) return;
            
            var exist = FindAnyObjectByType<CurrencyContainer>();
            if (exist != null)
            {
                Instance = exist;
                return;
            }
            
            var go = new GameObject("CurrencyContainer");
            Instance = go.AddComponent<CurrencyContainer>();
            DontDestroyOnLoad(go);
        }

        public string GetSaveData()
        {
            EnsureDefaults();
            
            var list = new List<CurrencyEntry>(_map.Count);
            foreach (var kv in _map)
                list.Add(new CurrencyEntry { type = kv.Key, value = kv.Value });
            
            return JsonUtility.ToJson(new SaveCollection { entries = list });
        }

        public void RestoreSaveData(string saveData)
        {
            _map.Clear();

            if (!string.IsNullOrEmpty(saveData))
            {
                var loaded = JsonUtility.FromJson<SaveCollection>(saveData);
                if (loaded.entries != null)
                {
                    foreach (var e in loaded.entries)
                    {
                        int v = clampToZero && e.value < 0 ? 0 : e.value;
                        _map[e.type] = v;
                    }
                }
            }

            EnsureDefaults();
            
            foreach (var kv in _map)
            {
                Bus<CurrencyChangedEvent>.Raise(
                    new CurrencyChangedEvent(kv.Key, kv.Value, kv.Value));
            }
        }
        
        public static void ResetAll()
        {
            EnsureInstance();
            Instance.ResetAllInternal(useDefault: false);
        }
        
        private void ResetAllInternal(bool useDefault)
        {
            foreach (var d in defaultCurrencies)
            {
                _map.TryGetValue(d.type, out int oldValue);
                int newValue = useDefault ? d.value : 0;

                if (clampToZero && newValue < 0)
                    newValue = 0;

                _map[d.type] = newValue;

                Bus<CurrencyChangedEvent>.Raise(
                    new CurrencyChangedEvent(d.type, oldValue, newValue));
            }

            if (raiseSaveEvt)
                Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
        }
        
    }
}