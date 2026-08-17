// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> BuffSkillAction -> BuffSkillAction -> DamageSkillAction
// Chain: None
//
// Original implementation:
// using System.Collections.Generic;
// using CIW.Code;
// using UnityEngine;
// using Work.YIS.Code.Buffs;
// using YIS.Code.Combat;
// using YIS.Code.Skills;
// using YIS.Code.Skills.Interfaces;
// using YIS.Code.Skills.Sequences;
//
// namespace PSB.Code.BattleCode.Skills.EnemySkills
// {
//     public class CurseLazerSkill : BaseSkill, IAttackSkill, IBuffOrDeBuffSkill
//     {
//         protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//
//             foreach (var target in targets)
//             {
//                 actions.Add(new BuffSkillAction(target, BuffType.ATTACK_DEBUFF, SkillData.damage, SkillData.durationTurn));
//                 actions.Add(new BuffSkillAction(target, BuffType.DEFENSE_DEBUFF, SkillData.damage, SkillData.durationTurn));
//             }
//
//             DamageData data = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
//             actions.Add(new DamageSkillAction(user, targets, data, SkillData.impulsePower));
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
//         }
//
//         public void UseBuffOrDeBuffSkill()
//         {
//         }
//
//     }
// }
