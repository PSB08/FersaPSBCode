using System;
using System.Collections.Generic;
using PSB.Code.BattleCode.Allies;
using UnityEngine;
using Work.CSH.Scripts.Relics;
using YIS.Code.Defines;
using YIS.Code.Items;
using YIS.Code.Skills;

namespace Work.PSB.Code.RunSystem
{
    public enum RunEventCostType
    {
        Currency,
        HealthFlat,
        HealthMaxPercent,
        Item,
        Relic,
        Skill,
        Ally
    }
    
    public enum RunEventSelectionSource
    {
        None,
        Skill,
        Relic,
        Item,
        Ally
    }
    
    [Serializable]
    public class RunEventCostData
    {
        public RunEventCostType costType;
        [Min(1)] public int amount = 1;
        [Range(0.01f, 1f)] public float maxHealthPercent = 0.1f;
        
        public ItemType currencyType = ItemType.Coin;
        public ItemDataSO item;
        public Relic relic;
        public SkillDataSO skill;
        public AllySO ally;
    }
    
    [Serializable]
    public class RunEventSelectionSettings
    {
        public RunEventSelectionSource source;
        public string title = "바칠 대상을 선택하세요";
        [Min(0)] public int minimumCount = 1;
        [Min(1)] public int maximumCount = 1;
        public bool consumeSelected = true;
        
        [Header("Skill Filter")]
        public bool useSkillCategoryFilter;
        public SkillCategory skillCategory = SkillCategory.Attack;
        [Min(0f)] public float minimumTotalDamage;
        [Min(0f)] public float maximumTotalDamage;
        [Min(0f)] public float maximumSingleDamage;
        public bool sortSkillsByDamageDescending = true;
        public List<SkillDataSO> allowedSkills = new List<SkillDataSO>();
        
        [Header("Other Filters")]
        [Min(1)] public int itemAmountPerSelection = 1;
        public List<ItemDataSO> allowedItems = new List<ItemDataSO>();
        public List<Relic> allowedRelics = new List<Relic>();
        public List<AllySO> allowedAllies = new List<AllySO>();
        
        public int MinimumCount => Mathf.Max(0, minimumCount);
        public int MaximumCount => Mathf.Max(MinimumCount, maximumCount);
    }
    
    public abstract class RunEventActionSO : ScriptableObject
    {
        [SerializeField] private string actionId;
        public List<RunEventCostData> costs = new List<RunEventCostData>();
        
        public string ActionId => string.IsNullOrWhiteSpace(actionId) ? name : actionId.Trim();
    }
}
