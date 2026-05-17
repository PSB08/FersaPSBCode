using UnityEngine;

namespace Work.PSB.Code.CoreSystem.UpgradeSystem
{
    [CreateAssetMenu(fileName = "HpUpgradeDefSO", menuName = "SO/Upgrade/HpDefSO", order = 10)]
    public class HealthUpgradeDefSO : UpgradeDefSO
    {
        [Header("10 레벨마다 추가로 요구되는 보너스 PP 비용")]
        public int extraCostPer10Levels = 100;

        public override int GetAddForLevel(int level)
        {
            int addValue = base.GetAddForLevel(level);
            return addValue;
        }
        
        public override int GetCostForLevel(int level)
        {
            int cost = base.GetCostForLevel(level);
            
            if (level % 10 == 0)
            {
                cost += extraCostPer10Levels;
            }
            
            return cost;
        }
        
    }
}