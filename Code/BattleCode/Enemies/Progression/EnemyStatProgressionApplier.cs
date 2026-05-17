using Code.Scripts.Entities;
using PSB_Lib.StatSystem;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    //강화를 적용하는 코드 
    public class EnemyStatProgressionApplier
    {
        private readonly EnemyProgressionSO _progression;
        private readonly int _stage;

        public EnemyStatProgressionApplier(EnemyProgressionSO progression, int stage)
        {
            _progression = progression;
            _stage = stage;
        }

        public void Apply(EntityStat statComp, StatOverride[] overrides)
        {
            if (_progression == null)
            {
                Debug.LogError("No progression found");
                return;
            }
            if (statComp == null)
            {
                Debug.LogError("No stat comp found");
                return;
            }
            if (overrides == null || overrides.Length == 0)
            {
                Debug.LogError("No stat overrides found");
                return;
            }

            for (int i = 0; i < overrides.Length; i++)
            {
                var ov = overrides[i];
                if (ov == null) continue;

                StatSO baseStat = ov.CreateStat();
                if (baseStat == null) continue;
                
                float mult = (_stage == 0) ? 1f : _progression.GetMultiplier(baseStat, _stage);
                if (Mathf.Approximately(mult, 1f)) continue;
                
                if (!statComp.TryGetStat(baseStat, out var runtimeStat) || runtimeStat == null)
                    continue;
                
                float add = runtimeStat.BaseValue * (mult - 1f);
                if (Mathf.Approximately(add, 0f)) continue;

                Debug.Log($"[Prog] stage={_stage} stat={runtimeStat.statName} " +
                          $"base={runtimeStat.BaseValue} mult={mult} add={add}");
                
                if (_stage == 0)
                {
                    runtimeStat.RemoveModifier(EnemyProgressionKey.Key);
                    continue;
                }
                
                runtimeStat.RemoveModifier(EnemyProgressionKey.Key);
                runtimeStat.AddModifier(EnemyProgressionKey.Key, add);
            }
        }

        public void Clear(EntityStat statComp, StatOverride[] overrides)
        {
            if (statComp == null) return;
            if (overrides == null || overrides.Length == 0) return;

            for (int i = 0; i < overrides.Length; i++)
            {
                var ov = overrides[i];
                if (ov == null) continue;

                StatSO baseStat = ov.CreateStat();
                if (baseStat == null) continue;

                if (!statComp.TryGetStat(baseStat, out var runtimeStat) || runtimeStat == null)
                    continue;

                runtimeStat.RemoveModifier(EnemyProgressionKey.Key);
            }
        }
        
    }
}
