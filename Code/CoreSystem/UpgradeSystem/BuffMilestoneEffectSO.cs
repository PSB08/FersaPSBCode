using CIW.Code;
using UnityEngine;
using Work.YIS.Code.Buffs;
using YIS.Code.Modules;

namespace Work.PSB.Code.CoreSystem.UpgradeSystem
{
    public abstract class MilestoneEffectSO : ScriptableObject
    {
        [TextArea] public string buffDescription;
        
        public abstract void ApplyEffect(Entity playerEntity);
    }
    
    [CreateAssetMenu(fileName = "BuffMilestoneSO", menuName = "SO/Upgrade/MilestoneSO", order = 0)]
    public class BuffMilestoneEffectSO : MilestoneEffectSO
    {
        public BuffType buffType;
        public float buffValue;
        public int buffDuration;

        public override void ApplyEffect(Entity playerEntity)
        {
            if (playerEntity == null) return;

            var buffModule = playerEntity.GetModule<BuffModule>();
            if (buffModule != null)
            {
                buffModule.BuffApply(buffType, buffValue, buffDuration);
            }
        }
        
    }
    
}