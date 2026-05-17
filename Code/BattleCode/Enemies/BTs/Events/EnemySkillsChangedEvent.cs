using PSW.Code.EventBus;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.BTs.Events
{
    public struct EnemySkillsChangedEvent : IEvent
    {
        public BattleEnemy Enemy;
        public SkillDataSO[] NewSkills;

        public EnemySkillsChangedEvent(BattleEnemy enemy, SkillDataSO[] newSkills)
        {
            Enemy = enemy;
            NewSkills = newSkills;
        }
        
    }
}