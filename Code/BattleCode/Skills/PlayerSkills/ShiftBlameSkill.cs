using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Skills.Sequences;
using YIS.Code.Combat;
using YIS.Code.Skills;
using YIS.Code.Skills.Interfaces;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.PlayerSkills
{
    public class ShiftBlameSkill : BaseSkill, IAttackSkill
    {
        protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, 
            IReadOnlyList<Entity> targets)
        {
            List<ISkillAction> actions = new List<ISkillAction>();
            actions.Add(new PlayEffectCallbackAction(this, targets));

            actions.Add(new BuffConditionSkillAction(user, BuffConditionType.HasAnyBuff, 
                (foundBuff) => new ISkillAction[] 
                {
                    new MultiTargetBuffSkillAction(targets, foundBuff, SkillData.damage * 0.5f, SkillData.durationTurn), // 적에게 뿌림
                    new BuffRemoveSkillAction(user, foundBuff, false)
                }
            ));

            DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
            actions.Add(new DamageSkillAction(user, targets, normalDamage, SkillData.impulsePower));

            return actions;
        }

        protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user,
            IReadOnlyList<Entity> targets)
        {
            return null;
        }

        public void UseAttackSkill()
        {
            
        }
        
    }
}