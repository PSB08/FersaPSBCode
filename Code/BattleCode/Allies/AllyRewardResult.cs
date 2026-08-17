using PSW.Code.EventBus;
using UnityEngine;

namespace PSB.Code.BattleCode.Allies
{
    public enum AllyRewardResultType
    {
        None,
        Acquired,
        Revived,
        Healed,
        AlreadyFullHealth,
    }
    
    public readonly struct AllyRewardResult
    {
        public readonly AllyRewardResultType Type;
        public readonly AllySO Ally;
        public readonly string AllyId;
        
        public readonly bool Changed;
        public readonly bool AutoEquipped;
        public readonly float HealAmount;
        public readonly float CurrentHp;
        public readonly float MaxHp;
        
        public bool IsRewardApplied => Type != AllyRewardResultType.None;
        public bool HasHealthData => MaxHp > 0f;
        
        public AllyRewardResult(AllyRewardResultType type, AllySO ally,
            string allyId, bool changed, bool autoEquipped,
            float healAmount, float currentHp, float maxHp)
        {
            Type = type;
            Ally = ally;
            AllyId = allyId;
            Changed = changed;
            AutoEquipped = autoEquipped;
            HealAmount = healAmount;
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }
        
        public static AllyRewardResult None(AllySO ally = null, string allyId = "")
        {
            return new AllyRewardResult(AllyRewardResultType.None, ally, allyId, false, false, 0f, 0f, 0f);
        }
    }

    public readonly struct AllyRewardResultEvent : IEvent
    {
        public readonly AllyRewardResult Result;
        public readonly string RewardKey;
        public readonly Vector3 WorldPosition;
        
        public AllyRewardResultEvent(AllyRewardResult result, string rewardKey, Vector3 worldPosition)
        {
            Result = result;
            RewardKey = rewardKey;
            WorldPosition = worldPosition;
        }
    }
    
}
