// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> DamageSkillAction -> SystemLogSkillAction -> BuffConditionSkillAction -> MultiTargetBuffSkillAction
// Chain: None
//
// Original implementation:
// using System.Collections.Generic;
// using CIW.Code;
// using PSB.Code.BattleCode.Events;
// using PSB.Code.BattleCode.Skills.Sequences;
// using YIS.Code.Combat;
// using YIS.Code.Skills;
// using YIS.Code.Skills.Interfaces;
// using YIS.Code.Skills.Sequences;
//
// namespace PSB.Code.BattleCode.Skills.PlayerSkills
// {
//     public class SpreadDebuffSkill : BaseSkill, IAttackSkill, IBuffOrDeBuffSkill
//     {
//         protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user,
//             IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//
//             DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
//             actions.Add(new DamageSkillAction(user, targets, normalDamage, SkillData.impulsePower));
//
//             const string linkId = "SpreadDebuff_Extra";
//
//             string logMessage =
//                 $"대상에게 버프 혹은 디버프가 있어 {SkillData.visualData.uiName}의 " +
//                 $"<link=\"{linkId}\"><u><color=#FFD966>추가 효과</color></u></link>가 발동했습니다.";
//
//             Dictionary<string, SystemLogLinkData> linkDataTable = new Dictionary<string, SystemLogLinkData>
//             {
//                 {
//                     linkId,
//                     new SystemLogLinkData(
//                         $"{SkillData.visualData.uiName} - 추가 효과",
//                         "대상에게 버프 혹은 디버프가 있을 때 발동합니다.\n" +
//                         "범위 내의 모든 대상에게 같은 버프 혹은 디버프를 부여합니다."
//                     )
//                 }
//             };
//
//             ISkillAction logAction = new SystemLogSkillAction(
//                 logMessage,
//                 SystemLogOwner.Player,
//                 linkDataTable
//             );
//
//             foreach (var target in targets)
//             {
//                 actions.Add(new BuffConditionSkillAction(target, BuffConditionType.HasAnyBuff,
//                     (foundBuff) => new ISkillAction[]
//                     {
//                         logAction,
//                         new MultiTargetBuffSkillAction(targets, foundBuff,
//                             SkillData.damage * 0.5f, SkillData.durationTurn)
//                     }
//                 ));
//             }
//
//             return actions;
//         }
//
//         protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user,
//             IReadOnlyList<Entity> targets)
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
