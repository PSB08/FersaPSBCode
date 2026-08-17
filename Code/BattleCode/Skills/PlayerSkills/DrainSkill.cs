// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> DamageSkillAction -> HealSkillAction
// Chain: PlayEffectCallbackAction -> DamageSkillAction -> HealSkillAction
//
// Original implementation:
// using System.Collections.Generic;
// using CIW.Code;
// using PSB.Code.BattleCode.Events;
// using PSB.Code.BattleCode.Skills.Interfaces;
// using PSB.Code.BattleCode.Skills.Sequences;
// using YIS.Code.Combat;
// using YIS.Code.Skills;
// using YIS.Code.Skills.Interfaces;
// using YIS.Code.Skills.Sequences;
//
// namespace PSB.Code.BattleCode.Skills.PlayerSkills
// {
//     public class DrainSkill : BaseSkill, IAttackSkill, IHealSkill
//     {
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
//             actions.Add(new HealSkillAction(user, SkillData.damage, HealMode.Flat));
//
//             return actions;
//         }
//
//         public void UseAttackSkill()
//         {
//         }
//
//     }
// }
