using System;
using System.Collections.Generic;
using PSB.Code.CoreSystem.Events;
using PSB.Code.CoreSystem.SaveSystem;
using PSB_Lib.Dependencies;
using PSB.Code.BattleCode.Entities;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PSB.Code.BattleCode.Allies
{
    [Serializable]
    public class AllyPartySaveData
    {
        public List<string> ownedAllyIds = new();
        public List<string> equippedAllyIds = new();
        public List<AllyHealthStateSaveData> allyHealthStates = new();
    }
    
    [Serializable]
    public class AllyHealthStateSaveData
    {
        public string allyId;
        public float currentHp;
        public float maxHp;
    }
    
    [DefaultExecutionOrder(-20)]
    [Provide]
    public class AllyPartyRepository : MonoBehaviour, ISaveable, IDependencyProvider
    {
        [SerializeField] private SaveId saveId;
        [SerializeField, Min(1)] private int maxEquippedAllies = 2;
        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField] private bool requestLoadOnStart = true;
        [SerializeField] private bool saveOnChange = true;
        
        private readonly List<string> _ownedAllyIds = new();
        private readonly List<string> _equippedAllyIds = new();
        private readonly Dictionary<string, AllyHealthStateSaveData> _healthStates = new();
        private AllyPartyService _service;
        
        [Inject] private BattleAllyManager allyManager;
        
        public static AllyPartyRepository Instance { get; private set; }
        public SaveId SaveId => saveId;
        public List<string> OwnedAllyIds => _ownedAllyIds;
        public List<string> EquippedAllyIds => _equippedAllyIds;
        public int MaxEquippedAllies => Mathf.Max(1, maxEquippedAllies);
        public AllyPartyService Service
        {
            get
            {
                _service ??= new AllyPartyService(this);
                return _service;
            }
        }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            _service = new AllyPartyService(this);
            
            if (persistAcrossScenes)
                DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            if (requestLoadOnStart)
                Bus<LoadPrefEvent>.Raise(new LoadPrefEvent());
        }
        
        private void Update()
        {
#if UNITY_EDITOR
            if (Keyboard.current.f4Key.wasPressedThisFrame)
            {
                foreach (BattleAlly ally in allyManager.GetAllies())
                {
                    if (ally == null || ally.IsDead) continue;
                    
                    EntityHealth health = ally.GetModule<EntityHealth>();
                    health?.HealByMaxPercent(0.2f);
                }
            }
#endif
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        
        public bool IsOwned(AllySO ally)
        {
            return ally != null && IsOwned(ally.AllyId);
        }
        
        public bool IsOwned(string allyId)
        {
            allyId = NormalizeId(allyId);
            return _ownedAllyIds.Contains(allyId);
        }
        
        public bool IsEquipped(AllySO ally)
        {
            return ally != null && IsEquipped(ally.AllyId);
        }
        
        public bool IsEquipped(string allyId)
        {
            allyId = NormalizeId(allyId);
            return _equippedAllyIds.Contains(allyId);
        }
        
        public bool TryGetHealth(string allyId, out float currentHp, out float maxHp)
        {
            allyId = NormalizeId(allyId);
            currentHp = 0f;
            maxHp = 0f;
            
            if (string.IsNullOrEmpty(allyId))
                return false;
            
            if (!_healthStates.TryGetValue(allyId, out AllyHealthStateSaveData state))
                return false;
            
            if (state == null || state.maxHp <= 0f)
                return false;
            
            currentHp = Mathf.Clamp(state.currentHp, 0f, state.maxHp);
            maxHp = state.maxHp;
            return true;
        }
        
        public bool IsDead(string allyId)
        {
            return TryGetHealth(allyId, out float currentHp, out _) && currentHp <= 0f;
        }
        
        public bool IsDead(AllySO ally)
        {
            return ally != null && IsDead(ally.AllyId);
        }
        
        public string GetSaveData()
        {
            AllyPartySaveData data = new()
            {
                ownedAllyIds = new List<string>(_ownedAllyIds),
                equippedAllyIds = new List<string>(_equippedAllyIds),
                allyHealthStates = new List<AllyHealthStateSaveData>(_healthStates.Values)
            };
            
            return JsonUtility.ToJson(data);
        }
        
        public void RestoreSaveData(string saveData)
        {
            _ownedAllyIds.Clear();
            _equippedAllyIds.Clear();
            _healthStates.Clear();
            
            if (string.IsNullOrEmpty(saveData))
                return;
            
            AllyPartySaveData data = JsonUtility.FromJson<AllyPartySaveData>(saveData);
            if (data == null)
                return;
            
            if (data.ownedAllyIds != null)
            {
                for (int i = 0; i < data.ownedAllyIds.Count; i++)
                    AddDistinct(_ownedAllyIds, NormalizeId(data.ownedAllyIds[i]));
            }
            
            RestoreHealthStates(data.allyHealthStates);
            
            if (data.equippedAllyIds == null)
                return;
            
            bool shouldFillEmptySlots = false;
            
            for (int i = 0; i < data.equippedAllyIds.Count; i++)
            {
                string allyId = NormalizeId(data.equippedAllyIds[i]);
                
                if (string.IsNullOrEmpty(allyId))
                {
                    shouldFillEmptySlots = true;
                    continue;
                }
                
                if (!_ownedAllyIds.Contains(allyId) || IsDead(allyId))
                {
                    shouldFillEmptySlots = true;
                    continue;
                }
                
                if (_equippedAllyIds.Count >= MaxEquippedAllies)
                    break;
                
                if (!AddDistinct(_equippedAllyIds, allyId))
                    shouldFillEmptySlots = true;
            }
            
            if (shouldFillEmptySlots)
                FillEmptyEquippedSlots();
        }
        
        public string NormalizeAllyId(string allyId)
        {
            return NormalizeId(allyId);
        }
        
        public bool AddOwnedAlly(string allyId)
        {
            allyId = NormalizeId(allyId);
            return AddDistinct(_ownedAllyIds, allyId);
        }
        
        public bool CanEquipAlly(string allyId)
        {
            allyId = NormalizeId(allyId);
            
            if (string.IsNullOrEmpty(allyId)) return false;
            if (!_ownedAllyIds.Contains(allyId)) return false;
            if (_equippedAllyIds.Contains(allyId)) return false;
            if (_equippedAllyIds.Count >= MaxEquippedAllies) return false;
            
            return true;
        }
        
        public bool AddEquippedAlly(string allyId)
        {
            allyId = NormalizeId(allyId);
            if (!CanEquipAlly(allyId))
                return false;
            
            _equippedAllyIds.Add(allyId);
            return true;
        }
        
        public bool RemoveEquippedAlly(string allyId)
        {
            allyId = NormalizeId(allyId);
            return _equippedAllyIds.Remove(allyId);
        }
        
        public bool RemoveOwnedAlly(string allyId)
        {
            allyId = NormalizeId(allyId);
            if (string.IsNullOrEmpty(allyId) || !_ownedAllyIds.Remove(allyId))
                return false;
            
            _equippedAllyIds.Remove(allyId);
            _healthStates.Remove(allyId);
            return true;
        }
        
        public bool SetHealth(string allyId, float currentHp, float maxHp)
        {
            allyId = NormalizeId(allyId);
            
            if (string.IsNullOrEmpty(allyId)) return false;
            if (float.IsNaN(currentHp) || float.IsInfinity(currentHp)) return false;
            if (float.IsNaN(maxHp) || float.IsInfinity(maxHp)) return false;
            
            maxHp = Mathf.Max(1f, maxHp);
            currentHp = Mathf.Clamp(currentHp, 0f, maxHp);
            
            bool removedFromEquipped = currentHp <= 0f && _equippedAllyIds.Remove(allyId);
            
            if (_healthStates.TryGetValue(allyId, out AllyHealthStateSaveData oldState) &&
                oldState != null &&
                Mathf.Approximately(oldState.currentHp, currentHp) &&
                Mathf.Approximately(oldState.maxHp, maxHp))
            {
                return removedFromEquipped;
            }
            
            _healthStates[allyId] = new AllyHealthStateSaveData
            {
                allyId = allyId,
                currentHp = currentHp,
                maxHp = maxHp
            };
            
            return true;
        }
        
        public bool ClearHealth(string allyId)
        {
            allyId = NormalizeId(allyId);
            return !string.IsNullOrEmpty(allyId) && _healthStates.Remove(allyId);
        }
        
        public bool ClearAllHealth()
        {
            if (_healthStates.Count == 0)
                return false;
            
            _healthStates.Clear();
            return true;
        }
        
        public void RequestSave()
        {
            if (saveOnChange)
                Bus<SavePrefEvent>.Raise(new SavePrefEvent(null));
        }
        
        private void RestoreHealthStates(List<AllyHealthStateSaveData> states)
        {
            if (states == null)
                return;
            
            for (int i = 0; i < states.Count; i++)
            {
                AllyHealthStateSaveData state = states[i];
                if (state == null)
                    continue;
                
                string allyId = NormalizeId(state.allyId);
                if (string.IsNullOrEmpty(allyId)) continue;
                if (state.maxHp <= 0f) continue;
                
                if (!_ownedAllyIds.Contains(allyId)) continue;
                if (float.IsNaN(state.currentHp) || float.IsInfinity(state.currentHp)) continue;
                if (float.IsNaN(state.maxHp) || float.IsInfinity(state.maxHp)) continue;
                
                state.allyId = allyId;
                state.currentHp = Mathf.Clamp(state.currentHp, 0f, state.maxHp);
                _healthStates[allyId] = state;
            }
        }
        
        private void FillEmptyEquippedSlots()
        {
            if (_equippedAllyIds.Count >= MaxEquippedAllies)
                return;
            
            for (int i = 0; i < _ownedAllyIds.Count; i++)
            {
                if (_equippedAllyIds.Count >= MaxEquippedAllies)
                    break;
                
                string allyId = NormalizeId(_ownedAllyIds[i]);
                
                if (string.IsNullOrEmpty(allyId))
                    continue;
                
                if (IsDead(allyId) || _equippedAllyIds.Contains(allyId))
                    continue;
                
                AddDistinct(_equippedAllyIds, allyId);
            }
        }
        
        private static bool AddDistinct(List<string> list, string allyId)
        {
            if (string.IsNullOrEmpty(allyId) || list.Contains(allyId))
                return false;
            
            list.Add(allyId);
            return true;
        }
        
        private static string NormalizeId(string allyId)
        {
            return string.IsNullOrWhiteSpace(allyId) ? string.Empty : allyId.Trim();
        }
        
    }
}
