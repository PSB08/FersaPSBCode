// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> DamageSkillAction -> SystemLogSkillAction -> BuffSkillAction -> SequenceSkillAction -> BuffConditionSkillAction
// Chain: None
//
// Original implementation:
// using System.Collections.Generic;
// using CIW.Code;
// using PSB.Code.BattleCode.Events;
// using PSB.Code.BattleCode.Skills.Sequences;
// using Work.YIS.Code.Buffs;
// using YIS.Code.Combat;
// using YIS.Code.Skills;
// using YIS.Code.Skills.Interfaces;
// using YIS.Code.Skills.Sequences;
//
// namespace PSB.Code.BattleCode.Skills.PlayerSkills
// {
//     public class BoostMoraleSkill : BaseSkill, IAttackSkill, IBuffOrDeBuffSkill
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
//             foreach (var target in targets)
//             {
//                 const string linkId = "BoostMoral_Extra";
//
//                 string logMessage =
//                     $"대상에게 공격력 감소가 있어 {SkillData.visualData.uiName}의 " +
//                     $"<link=\"{linkId}\"><u><color=#FFD966>추가 효과</color></u></link>가 발동했습니다.";
//
//                 Dictionary<string, SystemLogLinkData> linkDataTable = new Dictionary<string, SystemLogLinkData>
//                 {
//                     {
//                         linkId,
//                         new SystemLogLinkData(
//                             $"{SkillData.visualData.uiName} - 추가 효과",
//                             "대상에게 공격력 감소가 있을 때 발동합니다.\n" +
//                             "플레이어가 공격력 증가 버프를 획득합니다."
//                         )
//                     }
//                 };
//
//                 ISkillAction logAction = new SystemLogSkillAction(
//                     logMessage,
//                     SystemLogOwner.Player,
//                     linkDataTable
//                 );
//
//                 ISkillAction selfAtkBuff = new BuffSkillAction(user,
//                     BuffType.ATTACK_BUFF, SkillData.damage * 2, SkillData.durationTurn);
//
//                 ISkillAction onTrueAction = new SequenceSkillAction(
//                     new List<ISkillAction>
//                     {
//                         logAction,
//                         selfAtkBuff
//                     }
//                 );
//
//                 actions.Add(new BuffConditionSkillAction(target,
//                     BuffConditionType.HasSpecificBuff, BuffType.ATTACK_DEBUFF, onTrueAction, null));
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
