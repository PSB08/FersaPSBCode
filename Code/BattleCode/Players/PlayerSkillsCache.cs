using PSB.Code.BattleCode.Skills;
using UnityEngine;
using Work.YIS.Code.Skills;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Players
{
    public class PlayerSkillsCache : BaseSkillCache
    {
        [SerializeField] private SkillDataListSO skillList;

        protected override bool TryResolveSO(SkillEnum id, out SkillDataSO so)
        {
            so = null;
            if (skillList == null) return false;

            so = skillList.FindSkill(id);
            return so != null;
        }

        public bool TryGetOrCreate(SkillDataSO so, out BaseSkill skill)
        {
            skill = null;
            if (so == null) return false;
            return TryGetOrCreate((SkillEnum)so.index, so, out skill);
        }

        public void SetActive(SkillDataSO so, bool active)
        {
            if (so == null) return;
            SetActive((SkillEnum)so.index, active);
        }
        
    }
}
