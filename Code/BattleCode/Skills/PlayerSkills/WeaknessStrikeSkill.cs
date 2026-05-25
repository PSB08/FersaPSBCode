using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Skills.Sequences;
using Work.YIS.Code.Buffs;
using YIS.Code.Combat;
using YIS.Code.Skills;
using YIS.Code.Skills.Interfaces;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.PlayerSkills
{
    public class WeaknessStrikeSkill : BaseSkill, IAttackSkill
    {
        protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
        {
            List<ISkillAction> actions = new List<ISkillAction>();

            actions.Add(new PlayEffectCallbackAction(this, targets));

            foreach (var target in targets)
            {
                DamageData normalDamage = new DamageData(SkillData.damage, 
                    CurrentElementalState.CurrentElemental);
                ISkillAction onFalseAction = new DamageSkillAction(user, new List<Entity> 
                    { target }, normalDamage, SkillData.impulsePower);

                DamageData doubleDamage = new DamageData(SkillData.damage * 2f, 
                    CurrentElementalState.CurrentElemental);
                ISkillAction onTrueAction = new DamageSkillAction(user, new List<Entity> 
                    { target }, doubleDamage, SkillData.impulsePower);

                ISkillAction conditionAction = new BuffConditionSkillAction(target, 
                    BuffConditionType.HasSpecificBuff, BuffType.DEFENSE_DEBUFF, 
                    onTrueAction, onFalseAction);
                
                actions.Add(new BuffSkillAction(target,
                    BuffType.ATTACK_DEBUFF, SkillData.damage * 2, SkillData.durationTurn));

                actions.Add(conditionAction);
            }

            return actions;
        }

        protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
        {
            return null;
        }

        public void UseAttackSkill()
        {
            
        }
        
    }
}