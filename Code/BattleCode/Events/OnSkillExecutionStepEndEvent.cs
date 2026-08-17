using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Events
{
    public class OnSkillExecutionStepEndEvent : IEvent
    {
        public int SlotIndex;
        
        public OnSkillExecutionStepEndEvent(int slotIndex)
        {
            SlotIndex = slotIndex;
        }
    }
    
    public struct OnSetupSkillUIEvent : IEvent
    {
        public SkillDataSO[] SelectedSkills;
        
        public OnSetupSkillUIEvent(SkillDataSO[] selectedSkills)
        {
            SelectedSkills = selectedSkills;
        }
    }
    
    public struct OnSkillExecutionEndEvent : IEvent
    {
    }
    
}