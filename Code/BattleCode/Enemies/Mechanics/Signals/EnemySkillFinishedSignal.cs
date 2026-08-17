using PSB.Code.BattleCode.Skills;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemySkillFinishedSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
        public SkillDataSO Skill { get; }
        public BtSkillUseResult Result { get; }
        
        public EnemySkillFinishedSignal(BattleEnemy enemy, SkillDataSO skill, BtSkillUseResult result)
        {
            Enemy = enemy;
            Skill = skill;
            Result = result;
        }
        
    }
}
