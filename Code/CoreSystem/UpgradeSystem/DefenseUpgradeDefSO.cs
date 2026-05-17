using UnityEngine;

namespace Work.PSB.Code.CoreSystem.UpgradeSystem
{
    [CreateAssetMenu(fileName = "DefenseUpgradeDefSO", menuName = "SO/Upgrade/DefenseDefSO", order = 30)]
    public class DefenseUpgradeDefSO : UpgradeDefSO
    {
        [Space(10)]
        [Header("비용 설정")]
        public int costMultiplier = 2;
        
        [Space(10)]
        [Header("가속 성장 설정 (Accelerating)")]
        [Tooltip("만렙 시 추가될 총 방어력 (최종 150 - 기본 15 = 135)")]
        public int maxTotalAdd = 135;
        
        [Tooltip("가속도 (계산용이므로 float 유지)")]
        public float accelerationPower = 2f; 

        public override int GetCostForLevel(int level)
        {
            return base.GetCostForLevel(level) * costMultiplier; 
        }

        public override int GetTotalEnhancedAtLevel(int level)
        {
            level = Mathf.Clamp(level, 0, maxLevel);
            if (level <= 0) return 0;

            int guaranteedBase = level * 1;

            int remainingTotal = maxTotalAdd - (maxLevel * 1);

            float t = (float)level / maxLevel; 
            float curve = Mathf.Pow(t, accelerationPower);
            
            int curveBonus = Mathf.RoundToInt(remainingTotal * curve);

            return guaranteedBase + curveBonus; 
        }

        public override int GetAddForLevel(int level)
        {
            if (level <= 0) return 0;
            
            return GetTotalEnhancedAtLevel(level) - GetTotalEnhancedAtLevel(level - 1);
        }
        
    }
}