using System.Collections.Generic;
using CIW.Code;
using YIS.Code.CoreSystem;
using YIS.Code.Skills;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.Interfaces
{
    public interface ISkill
    {
        IReadOnlyList<ISkillAction> GenerateSkill(bool isChain, Entity user, IReadOnlyList<Entity> target);
    }
}