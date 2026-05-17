using System;
using System.Collections.Generic;
using System.Linq;
using CIW.Code;
using PSB_Lib.StatSystem;
using UnityEngine;
using YIS.Code.Modules;

namespace Code.Scripts.Entities
{
    public class EntityStat : MonoBehaviour, IModule
    {
        [SerializeField] private StatOverride[] statOverrides;
        private Dictionary<string, StatSO> _stats = new Dictionary<string, StatSO>();

        public Entity Owner { get; private set; }
        
        public event Action<StatSO> OnAnyStatValueChanged;

        public void Initialize(ModuleOwner owner)
        {
            Owner = owner as Entity;
            
            BuildStatsFrom(statOverrides);
        }

        public void OverrideStats(StatOverride[] overrides)
        {
            if (overrides == null || overrides.Length == 0)
                return;

            statOverrides = overrides;
            BuildStatsFrom(overrides);
        }

        private void BuildStatsFrom(StatOverride[] source)
        {
            if (source == null || source.Length == 0)
            {
                _stats.Clear();
                return;
            }
            _stats = source
                .Where(so => so != null)
                .ToDictionary(so => so.StatName, so => so.CreateStat());
        }
        
        public IEnumerable<StatSO> GetAllStats() => _stats.Values;

        public StatSO GetStat(StatSO stat)
        {
            Debug.Assert(stat != null, "Finding stat cannot be null");
            return _stats.GetValueOrDefault(stat.statName);
        }

        public bool TryGetStat(StatSO stat, out StatSO outStat)
        {
            Debug.Assert(stat != null, "Finding stat cannot be null");
            outStat = _stats.GetValueOrDefault(stat.statName);
            
            return outStat != null;
        }

        public bool TryGetStat(string statName, out StatSO outStat)
        {
            if (string.IsNullOrEmpty(statName))
            {
                outStat = null;
                return false;
            }

            outStat = _stats.GetValueOrDefault(statName);
            if (outStat == null)
                Debug.LogError($"{statName}이 없음");
            return outStat != null;
        }

        public void SetBaseValue(StatSO stat, float value)
        {
            StatSO s = GetStat(stat);
            if (s == null) return;

            s.BaseValue = value;
            OnAnyStatValueChanged?.Invoke(s);
        }

        public float GetBaseValue(StatSO stat)
            => GetStat(stat).BaseValue;

        public void IncreaseBaseValue(StatSO stat, float value)
        {
            StatSO s = GetStat(stat);
            if (s == null) return;

            s.BaseValue += value;
            OnAnyStatValueChanged?.Invoke(s);
        }

        public void AddModifier(StatSO stat, object key, float value)
        {
            StatSO s = GetStat(stat);
            if (s == null) return;

            s.AddModifier(key, value);
            OnAnyStatValueChanged?.Invoke(s);
        }

        public void RemoveModifier(StatSO stat, object key)
        {
            StatSO s = GetStat(stat);
            if (s == null) return;

            s.RemoveModifier(key);
            OnAnyStatValueChanged?.Invoke(s);
        }

        public void ClearAllStatModifier()
        {
            _stats.Values.ToList().ForEach(s => s.ClearModifier());
            
            OnAnyStatValueChanged?.Invoke(null);
        }

        public float SubscribeStat(StatSO stat, StatSO.ValueChangeHandler handler, float defaultValue)
        {
            StatSO target = GetStat(stat);
            if (target == null) return defaultValue;

            target.OnValueChanged += handler;
            return target.Value;
        }

        public void UnSubscribeStat(StatSO stat, StatSO.ValueChangeHandler handler)
        {
            StatSO target = GetStat(stat);
            if (target == null) return;

            target.OnValueChanged -= handler;
        }
        
    }
}