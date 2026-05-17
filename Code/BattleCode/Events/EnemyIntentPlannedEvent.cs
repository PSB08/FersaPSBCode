using PSB.Code.BattleCode.Enemies;
using PSW.Code.EventBus;
using Work.YIS.Code.Skills;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Events
{
    public struct EnemyIntentPlannedEvent : IEvent
    {
        public BattleEnemy enemy;
        public int skillIndex;
        public SkillEnum skillId;
        public  SkillDataSO skillSo;

        public EnemyIntentPlannedEvent(BattleEnemy enemy, int skillIndex, SkillEnum skillId, SkillDataSO skillSo)
        {
            this.enemy = enemy;
            this.skillIndex = skillIndex;
            this.skillId = skillId;
            this.skillSo = skillSo;
        }
    }
    
    public struct EnemyIntentClearedEvent : IEvent
    {
        public BattleEnemy enemy;
        public EnemyIntentClearedEvent(BattleEnemy enemy) => this.enemy = enemy;
    }
    
}