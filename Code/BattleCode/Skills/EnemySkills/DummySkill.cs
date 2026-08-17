// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> BuffSkillAction
// Chain: None
//
// Original implementation:
// using System.Collections.Generic;
// using CIW.Code;
// using UnityEngine;
// using Work.YIS.Code.Buffs;
// using YIS.Code.Modules;
// using YIS.Code.Skills;
// using YIS.Code.Skills.Interfaces;
// using YIS.Code.Skills.Sequences;
//
// namespace PSB.Code.BattleCode.Skills.EnemySkills
// {
//     public class DummySkill : BaseSkill, IAttackSkill
//     {
//         private BuffModule _buffModule;
//
//         protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//             actions.Add(new BuffSkillAction(user, BuffType.ATTACK_BUFF, SkillData.damage*2, 2));
//
//             UseAttackSkill();
//
//             return actions;
//         }
//
//         protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
//         {
//             return null;
//         }
//
//         public void UseAttackSkill()
//         {
//             //Debug.Log($"{owner.name} -> {_buffModule.name} : 더미공격슛");
//         }
//
//     }
// }
