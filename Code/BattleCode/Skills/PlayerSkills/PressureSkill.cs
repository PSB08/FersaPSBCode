using System.Collections.Generic;
using CIW.Code;
using Work.YIS.Code.Buffs;
using YIS.Code.Combat;
using YIS.Code.Skills;
using YIS.Code.Skills.Interfaces;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.PlayerSkills
{
    public class PressureSkill : BaseSkill, IAttackSkill
    {
        protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user,
            IReadOnlyList<Entity> targets)
        {
            List<ISkillAction> actions = new List<ISkillAction>();
            actions.Add(new PlayEffectCallbackAction(this, targets));

            DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
            actions.Add(new DamageSkillAction(user, targets, normalDamage, SkillData.impulsePower));

            return actions;
        }

        protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user, 
            IReadOnlyList<Entity> targets)
        {
            List<ISkillAction> actions = new List<ISkillAction>();
            actions.Add(new PlayEffectCallbackAction(this, targets));

            DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
            actions.Add(new DamageSkillAction(user, targets, normalDamage, SkillData.impulsePower));

            foreach (var target in targets)
            {
                actions.Add(new BuffSkillAction(target,
                    BuffType.ATTACK_DEBUFF, SkillData.damage * 2, SkillData.durationTurn));
            }

            return actions;
        }

        public void UseAttackSkill() { }
        
    }
}