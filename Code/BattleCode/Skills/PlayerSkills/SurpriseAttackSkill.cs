using System.Collections.Generic;
using CIW.Code;
using UnityEngine;
using YIS.Code.UI;
using YIS.Code.Skills;
using YIS.Code.Skills.Interfaces;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.PlayerSkills
{
    public class SurpriseAttackSkill : BaseSkill, IAttackSkill
    {
        protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
        {
            List<ISkillAction> actions = new List<ISkillAction>();
            
            DamageData damageData = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
            
            actions.Add(new PlayEffectCallbackAction(this, targets));
            actions.Add(new DamageSkillAction(user, targets, damageData, SkillData.impulsePower));
            
            UseAttackSkill();
            Debug.Log($"기습 액션! : {actions.Count}");
            return actions;
        }

        protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
        {
            Debug.Log("기습! 널");
            return null;
        }

        public void UseAttackSkill()
        {
            // var t = transform.root.Find("BuffModule");
            // _playerBuff = t ? t.GetComponent<BuffModule>() : null;
            // Debug.Assert(_playerBuff != null, "BuffModule is null in AtkDamageSkill OnChainAction");
            //
            // _playerBuff.BuffApply(BuffType.ATTACK_BUFF, SkillData.damage * 2, 
            //     SkillData.durationTurn);
            
            Debug.Log($"{SkillData.skillName} : {SkillData.range} 사거리 내 적에게 " +
                      $"{SkillData.damage}의 피해를 입힘");
        }
        
    }
}