using UnityEngine;

namespace Work.PSB.Code.CoreSystem.UpgradeSystem
{
    [CreateAssetMenu(fileName = "AttackUpgradeDefSO", menuName = "SO/Upgrade/AttackDefSO", order = 20)]
    public class AttackUpgradeDefSO : UpgradeDefSO
    {
        [Space(10)]
        [Header("레벨당 기본 비용 증가량")]
        public int costMultiplier = 5;

        // [강화 비용 설정]
        public override int GetCostForLevel(int level)
        {
            int baseCost = base.GetCostForLevel(level);
            
            return baseCost + costMultiplier;
        }
        
    }
}