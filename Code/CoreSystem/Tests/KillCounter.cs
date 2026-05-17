using System;
using System.Collections.Generic;
using PSB.Code.BattleCode.Enemies.BTs.Events;
using PSB.Code.BattleCode.Enums;
using PSB.Code.BattleCode.Events;
using PSB.Code.CoreSystem.Events;
using PSB.Code.CoreSystem.SaveSystem;
using PSW.Code.EventBus;
using UnityEngine;

namespace Work.PSB.Code.CoreSystem.Tests
{
    public class KillCounter : MonoBehaviour, ISaveable
    {
        public static KillCounter Instance { get; private set; }

        [field: SerializeField] public SaveId SaveId { get; private set; }
        
        private Dictionary<EnemyIdentity, int> _killCounts = new Dictionary<EnemyIdentity, int>();
        private int _totalKillCount = 0;
        
        private string _battleStartSnapshot;

        [Serializable]
        public class KillEntry
        {
            public EnemyIdentity identity;
            public int count;
        }
        
        [Serializable]
        public class KillSaveCollection
        {
            public int totalCount;
            public List<KillEntry> entries = new List<KillEntry>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Bus<RollbackBattleLootEvent>.OnEvent += OnRollbackTriggered;
            Bus<VillageResetEvent>.OnEvent += HandleVillageReset;
            Bus<EnemyKilledEvent>.OnEvent += HandleEnemyKilled;
        }

        private void HandleVillageReset(VillageResetEvent evt)
        {
            _killCounts.Clear();
            _totalKillCount = 0;
            _battleStartSnapshot = null;
            Debug.Log("<color=purple>마을 초기화 - 킬 카운터 리셋</color>");
        }

        private void OnDestroy()
        {
            Bus<RollbackBattleLootEvent>.OnEvent -= OnRollbackTriggered;
            Bus<VillageResetEvent>.OnEvent -= HandleVillageReset;
            Bus<EnemyKilledEvent>.OnEvent -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(EnemyKilledEvent evt)
        {
            EnemyIdentity id = evt.Identity;

            if (string.IsNullOrEmpty(id.EnemyName)) return;

            if (!_killCounts.ContainsKey(id)) _killCounts[id] = 0;
            _killCounts[id]++;
            
            _totalKillCount++;
            
            Debug.Log($"<color=navy>[Kill] {id.EnemyName}({id.Grade}) 처치 / " +
                      $"누적: {_killCounts[id]} - 전체: {_totalKillCount}</color>");
        }

        public int GetTotalKillCount() => _totalKillCount;
        
        public int GetExactKillCount(EnemyIdentity identity)
        {
            return _killCounts.TryGetValue(identity, out int count) ? count : 0;
        }

        public int GetKillCountByName(string enemyName)
        {
            int sum = 0;
            foreach (var kvp in _killCounts)
            {
                if (kvp.Key.EnemyName == enemyName) sum += kvp.Value;
            }
            return sum;
        }

        public int GetKillCountByGrade(EnemyGrade grade)
        {
            int sum = 0;
            foreach (var kvp in _killCounts)
            {
                if (kvp.Key.Grade == grade) sum += kvp.Value;
            }
            return sum;
        }
        
        public void TakeSnapshot()
        {
            _battleStartSnapshot = GetSaveData();
        }
        
        public void CommitKills()
        {
            _battleStartSnapshot = null;
        }
        
        private void OnRollbackTriggered(RollbackBattleLootEvent e)
        {
            if (string.IsNullOrEmpty(_battleStartSnapshot)) return;

            RestoreSaveData(_battleStartSnapshot);
            _battleStartSnapshot = null; 
            Debug.Log("<color=red>[KillCounter] 전투 포기: 데이터가 전투 전으로 롤백되었습니다.</color>");
        }
        
        public string GetSaveData()
        {
            var collection = new KillSaveCollection();
            collection.totalCount = _totalKillCount;
            
            foreach (var kvp in _killCounts)
            {
                collection.entries.Add(new KillEntry { identity = kvp.Key, count = kvp.Value });
            }
            
            return JsonUtility.ToJson(collection);
        }

        public void RestoreSaveData(string saveData)
        {
            _killCounts.Clear();
            _totalKillCount = 0;
            
            if (string.IsNullOrEmpty(saveData)) return;

            var loaded = JsonUtility.FromJson<KillSaveCollection>(saveData);
            
            if (loaded != null)
            {
                _totalKillCount = loaded.totalCount;
                
                foreach (var entry in loaded.entries)
                {
                    _killCounts[entry.identity] = entry.count;
                }
            }
        }
        
    }
}