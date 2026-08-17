using PSW.Code.EventBus;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Events
{
    public struct GiveSkillEvent : IEvent
    {
        public SkillDataSO Skill { get; private set; }

        public GiveSkillEvent(SkillDataSO skill)
        {
            Skill = skill;
        }
        
    }
}