using System.Collections.Generic;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public sealed class EnemyTurnPlanner
    {
        public int SelectDefenseSkill(EnemySkillExecutor skillExecutor, bool shouldPrioritizeDefense, bool isDangerous)
        {
            if (!shouldPrioritizeDefense || !isDangerous || skillExecutor == null)
                return -1;
            
            return skillExecutor.PickBestDefenseSkillIndex();
        }
        
        public int SelectDrawSkill(EnemySkillExecutor skillExecutor, bool canGainMoreSkills)
        {
            if (!canGainMoreSkills || skillExecutor == null)
                return -1;
            
            return skillExecutor.PickBestDrawSkillIndex();
        }
        
        public int SelectCostRecoverySkill(EnemySkillExecutor skillExecutor, bool needsCostRecovery)
        {
            if (!needsCostRecovery || skillExecutor == null)
                return -1;
            
            return skillExecutor.PickBestCostRecoverySkillIndex();
        }
        
        public int SelectBestActionSkill(EnemySkillExecutor skillExecutor)
        {
            return skillExecutor != null ? skillExecutor.PickBestActionSkillIndex() : -1;
        }
        
        public int SelectBestActionSkill(EnemySkillExecutor skillExecutor,
            IReadOnlyList<int> excludedIndices)
        {
            return skillExecutor != null
                ? skillExecutor.PickBestActionSkillIndexExcept(excludedIndices)
                : -1;
        }
        
    }
}
