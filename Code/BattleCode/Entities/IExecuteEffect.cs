using CIW.Code;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Entities
{
    public interface IExecuteEffect
    {
        void PlayEffectForTarget(BaseSkill skill, Transform targetTrans,
            Entity target, SkillDataSO vfxSourceData);
    }
}