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
    public class IntimidationSkill : BaseSkill, IAttackSkill
    {
        protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, 
            IReadOnlyList<Entity> targets)
        {
            List<ISkillAction> actions = new List<ISkillAction>();
            actions.Add(new PlayEffectCallbackAction(this, targets));

            foreach (var target in targets)
            {
                DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
                DamageData reducedDamage = new DamageData(SkillData.damage * 0.5f, CurrentElementalState.CurrentElemental);

                ISkillAction damageOnFalse = new DamageSkillAction(user, new List<Entity> { target }, normalDamage, SkillData.impulsePower);
                ISkillAction damageOnTrue = new DamageSkillAction(user, new List<Entity> { target }, reducedDamage, SkillData.impulsePower);

                actions.Add(new BuffConditionSkillAction(user, 
                    BuffConditionType.HasSpecificBuff, BuffType.ATTACK_BUFF, damageOnTrue, damageOnFalse));

                ISkillAction defDebuff = new BuffSkillAction(target, 
                    BuffType.DEFENSE_DEBUFF, SkillData.damage * 2, SkillData.durationTurn);
                actions.Add(new BuffConditionSkillAction(user, 
                    BuffConditionType.HasSpecificBuff, BuffType.ATTACK_BUFF, defDebuff, null));

                ISkillAction atkDebuff = new BuffSkillAction(target, 
                    BuffType.ATTACK_DEBUFF, SkillData.damage * 2, SkillData.durationTurn);
                actions.Add(new BuffConditionSkillAction(user, 
                    BuffConditionType.HasSpecificBuff, BuffType.ATTACK_BUFF, atkDebuff, null));
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