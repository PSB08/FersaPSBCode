using System;
using System.Collections.Generic;
using PSB_Lib.StatSystem;
using UnityEngine;
using YIS.Code.Defines;

namespace Work.PSB.Code.CoreSystem.UpgradeSystem
{
    public enum GrowthCurveType
    {
        Linear,
        Accelerating
    }
    
    [Serializable]
    public struct UpgradeMilestone
    {
        public int unlockLevel;
        public MilestoneEffectSO effect;
    }
    
    [CreateAssetMenu(fileName = "UpgradeDefSO", menuName = "SO/Upgrade/UpgradeDef", order = 0)]
    public class UpgradeDefSO : ScriptableObject
    {
        [Header("Target")]
        public StatSO targetStat;
        public int baseReferenceValue = 0;

        [Header("Display Base (강화 기준 기본값)")]
        public int maxLevel = 50;             // 최대 레벨
        public int targetMaxValue = 1000;  // 만렙 달성 시 최종 목표 수치
        public GrowthCurveType curveType = GrowthCurveType.Linear;

        [Header("Cost Currency")]
        public ItemType costType = ItemType.PP;
        public int startCost = 10;
        public int maxCost = 1000;
        public GrowthCurveType costCurveType = GrowthCurveType.Accelerating;
        
        [Header("Milestones")]
        public List<UpgradeMilestone> milestones = new List<UpgradeMilestone>();

        public virtual int GetAddForLevel(int level)
        {
            level = Mathf.Clamp(level, 0, maxLevel);
            if (level <= 0) return 0;

            // 이번 레벨까지의 총 누적량 - 전 레벨까지의 총 누적량
            int currentTotal = CalculateTotalEnhancementAt(level);
            int previousTotal = CalculateTotalEnhancementAt(level - 1);

            return currentTotal - previousTotal;
        }

        public virtual int GetTotalEnhancedAtLevel(int level)
        {
            level = Mathf.Clamp(level, 0, maxLevel);
            if (level <= 0) return 0;
            
            return CalculateTotalEnhancementAt(level);
        }
        
        private int CalculateTotalEnhancementAt(int level)
        {
            float totalNeeded = targetMaxValue - baseReferenceValue;
            
            float t = (float)level / maxLevel;
            
            switch (curveType)
            {
                case GrowthCurveType.Accelerating: 
                    t = Mathf.Pow(t, 1.5f);
                    break;
                case GrowthCurveType.Linear:       
                default:
                    break;
            }
            
            return Mathf.RoundToInt(totalNeeded * t);
        }
        
        public bool IsMax(int currentLevel) => currentLevel >= maxLevel;
        public int GetNextAdd(int currentLevel) => GetAddForLevel(currentLevel + 1);
        public int GetNextCost(int currentLevel) => GetCostForLevel(currentLevel + 1);

        public virtual int GetCostForLevel(int level)
        {
            level = Mathf.Clamp(level, 0, maxLevel);
            
            if (level <= 0) return 0;
            if (level == 1) return startCost;

            float t = (float)(level - 1) / (maxLevel - 1);

            switch (costCurveType)
            {
                case GrowthCurveType.Accelerating: 
                    t = Mathf.Pow(t, 1.5f);
                    break;
                case GrowthCurveType.Linear:       
                default:
                    break;
            }

            int costDiff = maxCost - startCost;
            return startCost + Mathf.RoundToInt(costDiff * t);
        }
        
    }
}