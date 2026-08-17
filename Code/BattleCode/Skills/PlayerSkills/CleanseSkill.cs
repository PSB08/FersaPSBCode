// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> DamageSkillAction -> HealSkillAction
// Chain: PlayEffectCallbackAction -> DamageSkillAction -> SystemLogSkillAction -> BuffRemoveSkillAction -> HealSkillAction
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
//     public class CleanseSkill : BaseSkill, IAttackSkill, IBuffOrDeBuffSkill, IHealSkill
//     {
//         private readonly BuffType[] _debuffsToClear = new BuffType[]
//         {
//             BuffType.DEFENSE_DEBUFF,
//             BuffType.ATTACK_DEBUFF,
//             BuffType.FLAME_DEBUFF,
//             BuffType.RAIN_DEBUFF
//         };
//
//         protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//
//             DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
//             actions.Add(new DamageSkillAction(user, targets, normalDamage, SkillData.impulsePower));
//             actions.Add(new HealSkillAction(user, SkillData.damage, HealMode.Flat));
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
//             const string linkId = "Cleanse_Extra";
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
//                         "체력을 2배로 회복하고, 디버프들을 제거합니다."
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
//             foreach (var debuff in _debuffsToClear)
//             {
//                 var removeAction = new BuffRemoveSkillAction(user, debuff);
//                 actions.Add(removeAction);
//             }
//
//             actions.Add(new HealSkillAction(user, SkillData.damage * 2, HealMode.Flat));
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
