using System.Collections;
using Code.Scripts.Entities;
using PSB_Lib.StatSystem;
using PSB.Code.BattleCode.Entities;
using UnityEngine;
using YIS.Code.Modules;

namespace Work.PSB.Code.CoreSystem.Buffs
{
    public class HpBuff : Buff
    {
        [SerializeField] private StatSO hpStat;
        private float _healValue;
        
        protected override void ApplyBuff(float value, int duration)
        {
            _healValue = value;
            
            var statModule = owner.GetModule<EntityStat>();
            if (statModule != null)
            {
                statModule.AddModifier(hpStat, this, _healValue);
                StartCoroutine(HealAfterStatUpdateDelay());
            }

            PlayEffect();
        }

        private IEnumerator HealAfterStatUpdateDelay()
        {
            yield return null; 

            var healthModule = owner.GetModule<EntityHealth>();
            if (healthModule != null)
            {
                healthModule.HealFlat(_healValue);
            }
        }

        protected override void RemoveBuff()
        {
            var healthModule = owner.GetModule<EntityHealth>();
            if (healthModule != null)
            {
                float finalHp = Mathf.Max(1f, healthModule.CurrentHealth - _healValue);
                healthModule.SetCurrentHealth(finalHp);
            }

            var statModule = owner.GetModule<EntityStat>();
            if (statModule != null)
            {
                statModule.RemoveModifier(hpStat, this);
            }

            StopEffect();
        }
        
    }
}