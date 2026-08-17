using Work.YIS.Code.Skills;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Entities
{
    public class EntitySkillPlan
    {
        public int Index { get; private set; } = -1;
        public SkillEnum Id { get; private set; }
        public SkillDataSO SkillData { get; private set; }
        public bool HasPlan => SkillData != null;
        
        public bool TrySet(int index, SkillDataSO skillData)
        {
            if (index < 0 || skillData == null)
                return false;
            
            Index = index;
            Id = (SkillEnum)skillData.index;
            SkillData = skillData;
            return true;
        }
        
        public bool TryGetSkillIds(out SkillEnum[] ids)
        {
            ids = null;
            
            if (!HasPlan)
                return false;
            
            ids = new[] { Id };
            return true;
        }
        
        public void Clear()
        {
            Index = -1;
            Id = default;
            SkillData = null;
        }
        
    }
}