using PSB.Code.BattleCode.Skills.Interfaces;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Skills.Buffs
{
    public class StealthBuff : Buff, ITargetingRuleProvider
    {
        public bool BlocksDirectTargeting => true;
        public bool BlocksRangeTargeting => false;

        protected override void ApplyBuff(float value, int duration)
        {
            PlayEffect();
        }

        protected override void RemoveBuff()
        {
            StopEffect();
        }
        
    }
}