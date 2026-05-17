using Code.Scripts.Entities;
using PSB.Code.CoreSystem.SaveSystem;
using UnityEngine;
using YIS.Code.Defines;

namespace Work.PSB.Code.CoreSystem.UpgradeSystem
{
    public class UpgradeService
    {
        private readonly EntityStat _stat;
        private readonly int _globalMaxLevel;

        public UpgradeService(EntityStat stat, int globalMaxLevel = 30)
        {
            _stat = stat;
            _globalMaxLevel = globalMaxLevel;
        }

        public int GetTotalGlobalLevel(UpgradeDefSO[] allDefs)
        {
            if (allDefs == null) return 0;
            int total = 0;
            foreach (var def in allDefs)
            {
                if (def != null) total += GetCurrentLevel(def);
            }
            return total;
        }

        public int GetRemainingGlobalLevel(UpgradeDefSO[] allDefs)
        {
            return Mathf.Max(0, _globalMaxLevel - GetTotalGlobalLevel(allDefs));
        }

        public bool TryUpgrade(UpgradeDefSO def, UpgradeDefSO[] allDefs)
        {
            if (def == null || def.targetStat == null) return false;
            if (_stat == null) return false;

            int currentGlobalLevel = GetTotalGlobalLevel(allDefs);
            if (currentGlobalLevel >= _globalMaxLevel)
                return false;

            int curLevel = GetCurrentLevel(def);
            if (def.IsMax(curLevel)) 
                return false;

            int cost = def.GetNextCost(curLevel);
            int add = def.GetNextAdd(curLevel);

            if (!CurrencyContainer.Spend(def.costType, cost))
                return false;

            _stat.IncreaseBaseValue(def.targetStat, add);
            return true;
        }

        public bool TryResetAllUpgrades(UpgradeDefSO[] defs, ItemType costType, int cost)
        {
            if (defs == null || _stat == null) return false;

            if (cost > 0)
            {
                if (!CurrencyContainer.Spend(costType, cost))
                    return false;
            }

            foreach (var def in defs)
            {
                if (def == null || def.targetStat == null) continue;
                _stat.SetBaseValue(def.targetStat, def.baseReferenceValue);
            }
            
            return true;
        }

        public void ResetUpgrade(UpgradeDefSO def)
        {
            if (def == null || def.targetStat == null) return;
            if (_stat == null) return;

            _stat.SetBaseValue(def.targetStat, def.baseReferenceValue);
        }
        
        public void ResetAllUpgrades(UpgradeDefSO[] defs)
        {
            if (defs == null) return;

            foreach (var def in defs)
            {
                ResetUpgrade(def);
            }
        }

        public int GetCurrentLevel(UpgradeDefSO def)
        {
            if (def == null || def.targetStat == null) return 0;

            float current = _stat.GetBaseValue(def.targetStat);
            float enhanced = current - def.baseReferenceValue;

            if (enhanced <= 0f) return 0;

            int calculatedLevel = 0;
            
            for (int i = 1; i <= def.maxLevel; i++)
            {
                if (enhanced >= def.GetTotalEnhancedAtLevel(i) - 0.001f) 
                {
                    calculatedLevel = i;
                }
                else
                {
                    break;
                }
            }
            
            return Mathf.Clamp(calculatedLevel, 0, def.maxLevel); 
        }
        
    }
}
