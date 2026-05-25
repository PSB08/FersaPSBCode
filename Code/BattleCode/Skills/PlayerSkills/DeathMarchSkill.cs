using System.Collections.Generic;
using CIW.Code;
using UnityEngine;
using YIS.Code.Combat;
using YIS.Code.Defines;
using YIS.Code.Skills;
using YIS.Code.Skills.Interfaces;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.PlayerSkills
{
    public class DeathMarchSkill : BaseSkill, IAttackSkill, IEnchantable
    {
        protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
        {
            List<ISkillAction> actions = new List<ISkillAction>();
            
            DamageData damageData = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
            
            actions.Add(new PlayEffectCallbackAction(this, targets));
            actions.Add(new DamageSkillAction(user, targets, damageData, SkillData.impulsePower));
            
            UseAttackSkill();
            return actions;
        }

        protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
        {
            List<ISkillAction> actions = new List<ISkillAction>();
            
            DamageData damageData = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
            
            actions.Add(new PlayEffectCallbackAction(this, targets));
            actions.Add(new DamageSkillAction(user, targets, damageData, SkillData.impulsePower));
            
            UseAttackSkill();
            return actions;
        }

        public void UseAttackSkill()
        {
            var atkElem = CurrentElementalState.CurrentElemental;
            Debug.Log($"[공격] 속성:{atkElem} / 사거리:{SkillData.range} / 피해:{SkillData.damage}");
        }

        public void ApplyEnchant(Elemental elemental)
        {
            ChangeElemental(elemental);
        }

        
    }
}