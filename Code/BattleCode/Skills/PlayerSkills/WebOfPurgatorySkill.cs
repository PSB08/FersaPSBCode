// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> DamageSkillAction
// Chain: SystemLogSkillAction -> SkillRangeChangeAction -> BuffSkillAction -> PlayEffectCallbackAction -> DamageSkillAction -> SkillRangeRevertAction
//
// Original implementation:
// using System.Collections.Generic;
// using CIW.Code;
// using PSB.Code.BattleCode.Events;
// using PSB.Code.BattleCode.Players;
// using PSB.Code.BattleCode.Skills.Sequences;
// using Work.YIS.Code.Buffs;
// using YIS.Code.Combat;
// using YIS.Code.Skills;
// using YIS.Code.Skills.Interfaces;
// using YIS.Code.Skills.Sequences;
//
// namespace PSB.Code.BattleCode.Skills.PlayerSkills
// {
//     public class WebOfPurgatorySkill : BaseSkill, IAttackSkill, IBuffOrDeBuffSkill
//     {
//         protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//
//             DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
//             actions.Add(new DamageSkillAction(user, targets, normalDamage, SkillData.impulsePower));
//
//             return actions;
//         }
//
//         protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//
//             int originalRange = SkillData.range;
//
//             actions.Add(new SystemLogSkillAction(
//                 $"체이닝 효과로 {SkillData.visualData.uiName}의 범위가 4로 증가했습니다.",
//                 SystemLogOwner.Player
//             ));
//
//             actions.Add(new SkillRangeChangeAction(SkillData, 4));
//
//             List<Entity> newTargets = new List<Entity>();
//             var selector = user.GetModule<PlayerTargetSelector>();
//
//             if (selector != null)
//             {
//                 var enemies = selector.GetEnemies();
//                 int centerIndex = selector.GetCurrentTargetIndex();
//
//                 newTargets = SkillTargetingUtil.GetTargetsByRange(enemies, centerIndex, 4, true);
//
//                 foreach (var target in newTargets)
//                 {
//                     float burnDmg = SkillData.damage * 0.5f;
//                     actions.Add(new BuffSkillAction(target, BuffType.FLAME_DEBUFF, burnDmg, 2));
//                 }
//             }
//
//             actions.Add(new PlayEffectCallbackAction(this, newTargets));
//
//             DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
//             actions.Add(new DamageSkillAction(user, newTargets, normalDamage, SkillData.impulsePower));
//
//             actions.Add(new SkillRangeRevertAction(SkillData, originalRange));
//
//             return actions;
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
