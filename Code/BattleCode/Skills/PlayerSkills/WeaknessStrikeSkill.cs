// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> DamageSkillAction -> DamageSkillAction -> SystemLogSkillAction -> SequenceSkillAction -> BuffConditionSkillAction -> BuffSkillAction -> BuffConditionSkillAction
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
//     public class WeaknessStrikeSkill : BaseSkill, IAttackSkill, IBuffOrDeBuffSkill
//     {
//         protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//
//             foreach (var target in targets)
//             {
//                 DamageData normalDamage = new DamageData(SkillData.damage,
//                     CurrentElementalState.CurrentElemental);
//                 ISkillAction onFalseAction = new DamageSkillAction(user, new List<Entity>
//                     { target }, normalDamage, SkillData.impulsePower);
//
//                 DamageData doubleDamage = new DamageData(SkillData.damage * 2f,
//                     CurrentElementalState.CurrentElemental);
//
//                 ISkillAction doubleDamageAction = new DamageSkillAction(user, new List<Entity>
//                     { target }, doubleDamage, SkillData.impulsePower);
//
//                 const string linkId = "Subjugate_Extra";
//
//                 string logMessage =
//                     $"이전 공격 스킬이 있어 {SkillData.visualData.uiName}의 " +
//                     $"<link=\"{linkId}\"><u><color=#FFD966>추가 효과</color></u></link>가 발동했습니다.";
//
//                 Dictionary<string, SystemLogLinkData> linkDataTable = new Dictionary<string, SystemLogLinkData>
//                 {
//                     {
//                         linkId,
//                         new SystemLogLinkData(
//                             $"{SkillData.visualData.uiName} - 추가 효과",
//                             "이전 공격 스킬이 있을 때 발동합니다.\n" +
//                             "대상에게 방어력 감소 디버프를 부여하고, 플레이어는 방어력 증가 버프와 체력 증가 버프를 획득합니다."
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
//                 ISkillAction onTrueAction = new SequenceSkillAction(
//                     new List<ISkillAction>
//                     {
//                         logAction,
//                         doubleDamageAction
//                     }
//                 );
//
//                 actions.Add(new BuffConditionSkillAction(target,
//                     BuffConditionType.HasSpecificBuff, BuffType.DEFENSE_DEBUFF,
//                     onTrueAction, onFalseAction));
//
//                 ISkillAction buffAction = new BuffSkillAction(target,
//                     BuffType.ATTACK_DEBUFF, SkillData.damage * 2, SkillData.durationTurn);
//                 actions.Add(new BuffConditionSkillAction(target,
//                     BuffConditionType.HasSpecificBuff, BuffType.DEFENSE_DEBUFF,
//                     buffAction, null));
//             }
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
