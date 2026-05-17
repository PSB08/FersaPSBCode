using System.Collections.Generic;
using CIW.Code;
using YIS.Code.CoreSystem;
using YIS.Code.Skills;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.Interfaces
{
    public interface ISkill
    {
        SkillDataSO SkillData { get; }
        IReadOnlyList<ISkillAction> ExecuteNormal(Context context, List<Entity> targets);
        IReadOnlyList<ISkillAction> ExecuteChain(Context context, List<Entity> targets);
        IReadOnlyList<ISkillAction> GenerateSkill(bool isChain, Entity user, IReadOnlyList<Entity> target);
        float GetFinalDamage();
    }
}