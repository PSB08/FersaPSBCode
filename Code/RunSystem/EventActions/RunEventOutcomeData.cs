using System;
using System.Collections.Generic;
using PSW.Code.Talk;
using UnityEngine;
using YIS.Code.Defines;

namespace Work.PSB.Code.RunSystem
{
    [Serializable]
    public class RunEventCurrencyChange
    {
        public ItemType currencyType = ItemType.Coin;
        public int amount;
    }
    
    [Serializable]
    public class RunEventOutcomeData
    {
        [Header("Talk")]
        public TalkDataListSO nextTalkData;
        public bool closeTalk;
        
        [Header("Reward")]
        public string rewardKey;
        
        [Header("Immediate Value Change")]
        public int healthFlatChange;
        [Range(-1f, 1f)] public float healthMaxPercentChange;
        public List<RunEventCurrencyChange> currencyChanges = new List<RunEventCurrencyChange>();
    }
}
