using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Entities;
using UnityEngine;
using YIS.Code.Combat;
using YIS.Code.Modules;
using YIS.Code.Skills;
using YIS.Code.Skills.Interfaces;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.EnemySkills
{
    public class EnemyHpHealSkill : BaseSkill, IAttackSkill
    {
        private EntityHealth _health;
        
        protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
        {
            List<ISkillAction> actions = new List<ISkillAction>();

            DamageData damageData = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);

           
            actions.Add(new PlayEffectCallbackAction(this, targets));
            actions.Add(new DamageSkillAction(user, targets, damageData, SkillData.impulsePower));
            actions.Add(new HealSkillAction(user, 0.1f, HealMode.MaxPercent));
            
            UseAttackSkill();

            return actions;
        }

        protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
        {
            return null;
        }

        public void UseAttackSkill()
        {
            var owner = GetComponentInParent<ModuleOwner>();
            if (owner == null)
            {
                Debug.LogError("EnemyHpHealSkill: ModuleOwner not found in parents.");
                return;
            }

            _health = owner.GetModule<EntityHealth>();
            if (_health == null)
            {
                Debug.LogError($"EnemyHpHealSkill: BuffModule not found under owner={owner.name}");
                return;
            }
            
            //Bus<EnemyHealRequest>.Raise(new EnemyHealRequest(_health, 0.1f, HealMode.MaxPercent));
            Debug.Log($"<color=white>{SkillData.skillName} : {SkillData.range} 사거리 내 적에게 " +
                      $"{SkillData.damage}의 피해를 입힘</color>");
        }
        
    }
}