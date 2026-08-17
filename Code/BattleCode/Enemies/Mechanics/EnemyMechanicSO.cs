using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Mechanics
{
    public abstract class EnemyMechanicSO : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private int priority = 100;
        
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public int Priority => priority;
        
        public abstract EnemyMechanicRuntime CreateRuntime(EnemyMechanicContext context);
        
        public virtual void Validate(EnemySO enemySO, EnemyMechanicValidationReport report)
        {
        }
        
        protected static bool ContainsEnemySkill(EnemySO enemySO, SkillDataSO skill)
        {
            if (enemySO == null || skill == null || enemySO.attackSkills == null)
                return false;
            
            for (int i = 0; i < enemySO.attackSkills.Length; i++)
            {
                if (ReferenceEquals(enemySO.attackSkills[i], skill))
                    return true;
            }
            
            return false;
        }
        
    }
}
