// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> DamageSkillAction -> DamageSkillAction -> SystemLogSkillAction -> SequenceSkillAction -> BuffConditionSkillAction -> BuffSkillAction -> BuffConditionSkillAction -> BuffSkillAction -> BuffConditionSkillAction
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
//     public class IntimidationSkill : BaseSkill, IAttackSkill, IBuffOrDeBuffSkill
//     {
//         protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user,
//             IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//
//             foreach (var target in targets)
//             {
//                 DamageData normalDamage = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
//                 DamageData reducedDamage = new DamageData(SkillData.damage * 0.5f, CurrentElementalState.CurrentElemental);
//
//                 ISkillAction damageOnFalse = new DamageSkillAction(user, new List<Entity> { target }, normalDamage, SkillData.impulsePower);
//                 ISkillAction damageOnTrue = new DamageSkillAction(user, new List<Entity> { target }, reducedDamage, SkillData.impulsePower);
//
//                 const string linkId = "Intimidation_Extra";
//
//                 string logMessage =
//                     $"플레이어에게 공격력 증가가 있어 {SkillData.visualData.uiName}의 " +
//                     $"<link=\"{linkId}\"><u><color=#FFD966>추가 효과</color></u></link>가 발동했습니다.";
//
//                 Dictionary<string, SystemLogLinkData> linkDataTable = new Dictionary<string, SystemLogLinkData>
//                 {
//                     {
//                         linkId,
//                         new SystemLogLinkData(
//                             $"{SkillData.visualData.uiName} - 추가 효과",
//                             "플레이어에게 공격력 증가 효과가 있을 때 발동합니다.\n" +
//                             "이 스킬의 피해량이 50%로 감소하는 대신, 대상에게 공격력 감소와 방어력 감소 디버프를 부여합니다."
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
//                 ISkillAction trueDamageSequence = new SequenceSkillAction(
//                     new List<ISkillAction>
//                     {
//                         logAction,
//                         damageOnTrue
//                     }
//                 );
//
//                 actions.Add(new BuffConditionSkillAction(user,
//                     BuffConditionType.HasSpecificBuff, BuffType.ATTACK_BUFF, trueDamageSequence, damageOnFalse));
//
//                 ISkillAction defDebuff = new BuffSkillAction(target,
//                     BuffType.DEFENSE_DEBUFF, SkillData.damage * 2, SkillData.durationTurn);
//                 actions.Add(new BuffConditionSkillAction(user,
//                     BuffConditionType.HasSpecificBuff, BuffType.ATTACK_BUFF, defDebuff, null));
//
//                 ISkillAction atkDebuff = new BuffSkillAction(target,
//                     BuffType.ATTACK_DEBUFF, SkillData.damage * 2, SkillData.durationTurn);
//                 actions.Add(new BuffConditionSkillAction(user,
//                     BuffConditionType.HasSpecificBuff, BuffType.ATTACK_BUFF, atkDebuff, null));
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
