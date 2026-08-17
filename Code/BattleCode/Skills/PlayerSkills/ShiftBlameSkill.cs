// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> SystemLogSkillAction -> BuffConditionSkillAction -> MultiTargetBuffSkillAction -> BuffRemoveSkillAction -> DamageSkillAction
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
//     public class ShiftBlameSkill : BaseSkill, IAttackSkill, IBuffOrDeBuffSkill
//     {
//         protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user,
//             IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//
//             const string linkId = "ShiftBlame_Extra";
//
//             string logMessage =
//                 $"플레이어에게 버프 혹은 디버프가 있어 {SkillData.visualData.uiName}의 " +
//                 $"<link=\"{linkId}\"><u><color=#FFD966>추가 효과</color></u></link>가 발동했습니다.";
//
//             Dictionary<string, SystemLogLinkData> linkDataTable = new Dictionary<string, SystemLogLinkData>
//             {
//                 {
//                     linkId,
//                     new SystemLogLinkData(
//                         $"{SkillData.visualData.uiName} - 추가 효과",
//                         "플레이어에게 버프 혹은 디버프가 있을 때 발동합니다.\n" +
//                         "그 버프 혹은 디버프를 제거한 뒤, 적들에게 부여합니다."
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
//             actions.Add(new BuffConditionSkillAction(user, BuffConditionType.HasAnyBuff,
//                 (foundBuff) => new ISkillAction[]
//                 {
//                     logAction,
//                     new MultiTargetBuffSkillAction(targets, foundBuff, SkillData.damage * 0.5f, SkillData.durationTurn),
//                     new BuffRemoveSkillAction(user, foundBuff)
//                 }
//             ));
//
//             DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
//             actions.Add(new DamageSkillAction(user, targets, normalDamage, SkillData.impulsePower));
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
