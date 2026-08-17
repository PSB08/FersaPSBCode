using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemySkillStartedSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
        public SkillDataSO Skill { get; }
        
        public EnemySkillStartedSignal(BattleEnemy enemy, SkillDataSO skill)
        {
            Enemy = enemy;
            Skill = skill;
        }
        
    }
}
