using System.Collections.Generic;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemySkillsChangedSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
        public IReadOnlyList<SkillDataSO> Skills { get; }
        public bool IsBattleStart { get; }
        
        public EnemySkillsChangedSignal(BattleEnemy enemy, SkillDataSO[] skills, bool isBattleStart)
        {
            Enemy = enemy;
            Skills = skills;
            IsBattleStart = isBattleStart;
        }
        
    }
}
