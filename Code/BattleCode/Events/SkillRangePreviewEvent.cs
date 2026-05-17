using PSW.Code.EventBus;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Events
{
    public struct SkillRangePreviewEvent : IEvent
    {
        public int Range;

        public SkillRangePreviewEvent(int range) => Range = range;
        
    }
    
    public struct SkillDamagePreviewEvent : IEvent
    {
        public SkillDataSO[] SkillDatas;
        public bool[] SetSkillIndex;
        public int PreviewSlotIndex;

        public SkillDamagePreviewEvent(SkillDataSO[] skillDatas, bool[] setSkillIndex, int previewSlotIndex)
        {
            SkillDatas = skillDatas != null ? (SkillDataSO[])skillDatas.Clone() : new SkillDataSO[0];
            SetSkillIndex = setSkillIndex != null ? (bool[])setSkillIndex.Clone() : new bool[0];
            PreviewSlotIndex = previewSlotIndex;
        }
    }
    
}