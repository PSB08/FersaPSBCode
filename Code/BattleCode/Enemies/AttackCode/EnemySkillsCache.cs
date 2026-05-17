using PSB.Code.BattleCode.Skills;
using Work.YIS.Code.Skills;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public class EnemySkillsCache : BaseSkillCache
    {
        protected override bool TryResolveSO(SkillEnum id, out SkillDataSO so)
        {
            so = null;
            return false;
        }

        public void Prewarm(SkillDataSO[] attackSkills)
        {
            if (attackSkills == null || attackSkills.Length == 0) return;

            for (int i = 0; i < attackSkills.Length; i++)
            {
                var so = attackSkills[i];
                if (so == null || so.skillPrefab == null) continue;

                var id = (SkillEnum)so.index;
                TryGetOrCreate(id, so, out _);
            }

            SetAllInactive();
        }
        
    }
}