// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> DamageSkillAction -> HealSkillAction -> BuffSkillAction
// Chain: SystemLogSkillAction -> BuffSkillAction -> HealSkillAction
//
// Original implementation:
// using System.Collections.Generic;
// using CIW.Code;
// using PSB.Code.BattleCode.Events;
// using PSB.Code.BattleCode.Skills.Interfaces;
// using PSB.Code.BattleCode.Skills.Sequences;
// using Work.YIS.Code.Buffs;
// using YIS.Code.Combat;
// using YIS.Code.Skills;
// using YIS.Code.Skills.Interfaces;
// using YIS.Code.Skills.Sequences;
//
// namespace PSB.Code.BattleCode.Skills.PlayerSkills
// {
//     public class UndyingEmberSkill : BaseSkill, IAttackSkill, IHealSkill, IBuffOrDeBuffSkill
//     {
//         protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//
//             SkipDamageThisCast = false;
//
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//
//             DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
//             actions.Add(new DamageSkillAction(user, targets, normalDamage, SkillData.impulsePower));
//             actions.Add(new HealSkillAction(user, SkillData.damage, HealMode.Flat));
//
//             actions.Add(new BuffSkillAction(user, BuffType.DEFENSE_BUFF, SkillData.damage, 1));
//
//             return actions;
//         }
//
//         protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//
//             SkipDamageThisCast = true;
//
//             const string linkId = "UndyingEmber_Extra";
//
//             string logMessage =
//                 $"양쪽 힐 스킬이 존재해서 {SkillData.visualData.uiName}의 " +
//                 $"<link=\"{linkId}\"><u><color=#FFD966>추가 효과</color></u></link>가 발동했습니다.";
//
//             Dictionary<string, SystemLogLinkData> linkDataTable = new Dictionary<string, SystemLogLinkData>
//             {
//                 {
//                     linkId,
//                     new SystemLogLinkData(
//                         $"{SkillData.visualData.uiName} - 추가 효과",
//                         "양쪽 힐 스킬이 존재할 때 발동합니다.\n" +
//                         "방어력 버프 대신 불사 버프를 획득하며, 체력을 2배로 회복합니다."
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
//             actions.Add(new BuffSkillAction(user, BuffType.UNDYING_BUFF, 0f, 1));
//             actions.Add(new HealSkillAction(user, SkillData.damage * 2f, HealMode.Flat));
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
//         public override bool SkipAnim(bool isChain)
//         {
//             return isChain;
//         }
//
//     }
// }
