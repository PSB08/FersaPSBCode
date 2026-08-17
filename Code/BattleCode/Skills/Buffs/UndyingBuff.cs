using PSB.Code.BattleCode.Entities;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Skills.Buffs
{
    public class UndyingBuff : Buff
    {
        protected override void ApplyBuff(float value, int duration)
        {
            var healthModule = owner.GetModule<EntityHealth>();
            if (healthModule != null)
            {
                healthModule.AddUndyingModifier(this);
            }

            PlayEffect();
        }

        protected override void RemoveBuff()
        {
            var healthModule = owner.GetModule<EntityHealth>();
            if (healthModule != null)
            {
                healthModule.RemoveUndyingModifier(this);
            }

            StopEffect();
        }
        
    }
}