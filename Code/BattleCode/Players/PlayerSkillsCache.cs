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

        public bool TryGetSkillData(SkillEnum id, out SkillDataSO so)
        {
            return TryResolveSO(id, out so);
        }

        public void SetActive(SkillDataSO so, bool active)
        {
            if (so == null) return;
            SetActive((SkillEnum)so.index, active);
        }
    }
}
