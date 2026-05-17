using System.Collections.Generic;
using CIW.Code;
using UnityEngine;
using YIS.Code.UI;
using YIS.Code.Skills;
using YIS.Code.Skills.Interfaces;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.EnemySkills
{
    public class FireArrowSkill : BaseSkill, IAttackSkill
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
            return null;
        }

        public void UseAttackSkill()
        {
            Debug.Log($"<color=red>{SkillData.skillName} : {SkillData.range} 사거리 내 적에게 " +
                      $"{SkillData.damage}의 피해를 입힘</color>");
        }
        
    }
}