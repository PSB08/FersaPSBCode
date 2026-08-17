// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> DamageSkillAction
// Chain: PlayEffectCallbackAction -> DamageSkillAction -> SystemLogSkillAction -> BuffSkillAction -> BuffSkillAction -> BuffSkillAction
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
//     public class SubjugateSkill : BaseSkill, IAttackSkill, IBuffOrDeBuffSkill
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
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//
//             DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
//             actions.Add(new DamageSkillAction(user, targets, normalDamage, SkillData.impulsePower));
//
//             const string linkId = "Subjugate_Extra";
//
//             string logMessage =
//                 $"이전 공격 스킬이 있어 {SkillData.visualData.uiName}의 " +
//                 $"<link=\"{linkId}\"><u><color=#FFD966>추가 효과</color></u></link>가 발동했습니다.";
//
//             Dictionary<string, SystemLogLinkData> linkDataTable = new Dictionary<string, SystemLogLinkData>
//             {
//                 {
//                     linkId,
//                     new SystemLogLinkData(
//                         $"{SkillData.visualData.uiName} - 추가 효과",
//                         "이전 공격 스킬이 있을 때 발동합니다.\n" +
//                         "대상에게 방어력 감소 디버프를 부여하고, 플레이어는 방어력 증가 버프와 체력 증가 버프를 획득합니다."
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
//             actions.Add(logAction);
//
//             foreach (var target in targets)
//             {
//                 actions.Add(new BuffSkillAction(target, BuffType.DEFENSE_DEBUFF, SkillData.damage, SkillData.durationTurn));
//             }
//
//             actions.Add(new BuffSkillAction(user, BuffType.DEFENSE_BUFF, SkillData.damage, SkillData.durationTurn));
//             actions.Add(new BuffSkillAction(user, BuffType.HP_BUFF, SkillData.damage, SkillData.durationTurn));
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
