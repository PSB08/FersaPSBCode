using System.Collections.Generic;
using PSB_Lib.StatSystem;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.Mechanics;
using PSB.Code.BattleCode.Enums;
using PSB.Code.BattleCode.Items;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies
{
    [CreateAssetMenu(fileName = "EnemySO", menuName = "SO/Enemy/EnemySO", order = 110)]
    public class EnemySO : ScriptableObject
    {
        [Header("Visual")]
        public Sprite icon;
        public RuntimeAnimatorController animController;
        
        [Header("Enemy inform")]
        public string enemyName;
        public EnemyGrade grade;
        public bool isRanged;
        
        [Header("Stat Overrides")]
        public StatOverride[] statOverrides;
        
        [Header("Progression")]
        public EnemyProgressionSO progressionSO;
        
        [Header("Drop")]
        public DropTableSO dropTable;
        
        [Header("Attack Skills")]
        public SkillDataSO[] attackSkills;
        
        [Header("Enemy AI Resources")]
        [Min(1)] public int maxCost = 10;
        [Min(1)] public int maxHandSkillCount = 5;
        [Min(1)] public int maxOwnedSkillCount = 10;
        
        [Header("Enemy Mechanics")]
        public EnemyMechanicSetSO mechanicSet;
        
        public bool IsOverOwnedSkillLimit(SkillDataSO[] skills, out int assignedCount, out int maxCount)
        {
            assignedCount = CountAssignedSkills(skills);
            maxCount = Mathf.Max(1, maxOwnedSkillCount);
            return assignedCount > maxCount;
        }
        
        public EnemyMechanicValidationReport BuildMechanicValidationReport()
        {
            if (mechanicSet == null)
                return new EnemyMechanicValidationReport();
            
            return mechanicSet.ValidateFor(this);
        }
        
        private static int CountAssignedSkills(SkillDataSO[] skills)
        {
            if (skills == null) return 0;
            
            int count = 0;
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null) count++;
            }
            
            return count;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxCost <= 0) maxCost = 10;
            if (maxHandSkillCount <= 0) maxHandSkillCount = 5;
            if (maxOwnedSkillCount <= 0) maxOwnedSkillCount = 10;
            
            if (IsOverOwnedSkillLimit(attackSkills, out int assignedCount, out int maxCount))
            {
                Debug.LogWarning($"[EnemySO] {name} attackSkills count is {assignedCount}, but maxOwnedSkillCount is {maxCount}.", this);
            }
            
            EnemyMechanicValidationReport report = BuildMechanicValidationReport();
            
            for (int i = 0; i < report.Errors.Count; i++)
            {
                Debug.LogError($"[EnemySO] {name} : {report.Errors[i]}", this);
            }
            
            for (int i = 0; i < report.Warnings.Count; i++)
            {
                Debug.LogWarning($"[EnemySO] {name} : {report.Warnings[i]}", this);
            }
        }
#endif
        
    }
}
