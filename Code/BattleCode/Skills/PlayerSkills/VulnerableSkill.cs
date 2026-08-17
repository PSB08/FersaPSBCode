// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> DamageSkillAction
// Chain: BuffSkillAction
//
// Original implementation:
// using System.Collections.Generic;
// using CIW.Code;
// using Work.YIS.Code.Buffs;
// using YIS.Code.Combat;
// using YIS.Code.Modules;
// using YIS.Code.Skills;
// using YIS.Code.Skills.Interfaces;
// using YIS.Code.Skills.Sequences;
//
// namespace PSB.Code.BattleCode.Skills.PlayerSkills
// {
//     public class VulnerableSkill : BaseSkill, IBuffOrDeBuffSkill, IAttackSkill
//     {
//         BuffModule _playerBuff;
//
//         protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user,
//             IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//
//             DamageData damageData = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//             actions.Add(new DamageSkillAction(user, targets, damageData, SkillData.impulsePower));
//
//             return actions;
//         }
//
//         protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user,
//             IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//
//             //actions.Add(new PlayEffectCallbackAction(this, targets));
//
//             foreach (var target in targets)
//             {
//                 actions.Add(new BuffSkillAction(target,
//                     BuffType.DEFENSE_DEBUFF, SkillData.damage * 2, SkillData.durationTurn));
//             }
//
//             return actions;
//         }
//
//         public void UseAttackSkill()
//         {
//
//         }
//
//         public void UseBuffOrDeBuffSkill()
//         {
//
//         }
//
//     }
// }
