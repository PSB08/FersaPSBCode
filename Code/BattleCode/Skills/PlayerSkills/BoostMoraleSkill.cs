using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Skills.Sequences;
using Work.YIS.Code.Buffs;
using YIS.Code.Skills;
using YIS.Code.Skills.Interfaces;
using YIS.Code.Skills.Sequences;
using YIS.Code.UI;

namespace PSB.Code.BattleCode.Skills.PlayerSkills
{
    public class BoostMoraleSkill : BaseSkill, IAttackSkill
    {
        protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, 
            IReadOnlyList<Entity> targets)
        {
            List<ISkillAction> actions = new List<ISkillAction>();
            actions.Add(new PlayEffectCallbackAction(this, targets));

            DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
            actions.Add(new DamageSkillAction(user, targets, normalDamage, SkillData.impulsePower));

            foreach (var target in targets)
            {
                ISkillAction selfAtkBuff = new BuffSkillAction(user, 
                    BuffType.ATTACK_BUFF, SkillData.damage * 2, SkillData.durationTurn);
                
                actions.Add(new BuffConditionSkillAction(target, 
                    BuffConditionType.HasSpecificBuff, BuffType.ATTACK_DEBUFF, selfAtkBuff, null));
            }

            return actions;
        }

        protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user, 
            IReadOnlyList<Entity> targets)
        {
            return null;
        }

        public void UseAttackSkill() { }
        
    }
}