using Code.Scripts.Entities;
using PSB_Lib.StatSystem;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.CoreSystem.SaveSystem
{
    public class PlayerStatPersistence : MonoBehaviour, IModule
    {
        private EntityStat _stat;

        private bool _restoreApplied;
        private bool _subscribed;

        private PlayerStatSaveData _loaded;

        public void Initialize(ModuleOwner owner)
        {
            _stat ??= owner.GetModule<EntityStat>();
        }

        private void Awake()
        {
            _stat ??= GetComponent<EntityStat>() ?? GetComponentInChildren<EntityStat>(true);
        }

        private void OnEnable()
        {
            _restoreApplied = false;

            PlayerStatSave.TryLoad(out _loaded);
            TrySubscribe();
        }

        private void Start()
        {
            ApplyRestoreIfNeeded();
            SaveSnapshot();
        }

        private void OnDisable()
        {
            if (_stat != null && _subscribed)
            {
                _stat.OnAnyStatValueChanged -= OnAnyStatChanged;
                _subscribed = false;
            }
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (_stat == null) return;

            _stat.OnAnyStatValueChanged += OnAnyStatChanged;
            _subscribed = true;
        }

        private void ApplyRestoreIfNeeded()
        {
            if (_restoreApplied) return;
            if (_stat == null) return;
            if (_loaded == null || _loaded.stats == null || _loaded.stats.Count == 0) return;

            _restoreApplied = true;
            
            foreach (var snap in _loaded.stats)
            {
                if (string.IsNullOrEmpty(snap.statName)) continue;

                if (_stat.TryGetStat(snap.statName, out var runtimeStat) && runtimeStat != null)
                {
                    runtimeStat.BaseValue = snap.baseValue;
                }
            }
        }

        private void OnAnyStatChanged(StatSO _)
        {
            ApplyRestoreIfNeeded();
            SaveSnapshot();
        }

        private void SaveSnapshot()
        {
            if (_stat == null) return;

            var data = new PlayerStatSaveData();
            foreach (var s in _stat.GetAllStats())
            {
                if (s == null) continue;
                
                data.stats.Add(new StatSnapshot
                {
                    statName = s.statName,
                    baseValue = s.BaseValue
                });
            }

            PlayerStatSave.Save(data);
        }
        
    }
}