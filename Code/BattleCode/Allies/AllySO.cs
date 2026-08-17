using PSB_Lib.StatSystem;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Allies
{
    [CreateAssetMenu(fileName = "AllySO", menuName = "SO/Ally/AllySO", order = 120)]
    public class AllySO : ScriptableObject
    {
        [SerializeField] private string allyId;
        
        public string AllyId => string.IsNullOrWhiteSpace(allyId) ? name : allyId;
        
        [Header("Visual")]
        public Sprite icon;
        public RuntimeAnimatorController animController;
        
        [Header("Name")]
        public string allyName;
        
        [Header("Stat Overrides")]
        public StatOverride[] statOverrides;
        
        [Header("Attack Skills")]
        public SkillDataSO[] attackSkills;
        
        [Header("Ally AI Resources")]
        [Min(1)] public int maxCost = 10;
        [Min(1)] public int maxOwnedSkillCount = 10;
        
        public bool IsOverOwnedSkillLimit(SkillDataSO[] skills, out int assignedCount, out int maxCount)
        {
            assignedCount = CountAssignedSkills(skills);
            maxCount = Mathf.Max(1, maxOwnedSkillCount);
            return assignedCount > maxCount;
        }
        
        private static int CountAssignedSkills(SkillDataSO[] skills)
        {
            if (skills == null)
                return 0;
            
            int count = 0;
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null)
                    count++;
            }
            
            return count;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxCost <= 0) maxCost = 10;
            if (maxOwnedSkillCount <= 0) maxOwnedSkillCount = 10;
            
            if (IsOverOwnedSkillLimit(attackSkills, out int assignedCount, out int maxCount))
            {
                Debug.LogWarning($"[AllySO] {name} : 공격 스킬 수가 제한을 넘었습니다. 현재 = {assignedCount}, 최대 = {maxCount}", this);
            }
        }
        
#endif
        
    }
}
