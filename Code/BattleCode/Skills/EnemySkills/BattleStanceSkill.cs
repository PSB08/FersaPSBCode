// Legacy BaseSkill implementation is fully commented out.
// Action construction order; conditional and loop-created actions follow declaration order.
// Normal: PlayEffectCallbackAction -> DamageSkillAction -> NextFrameSkillAction -> BuffSkillAction
// Chain: PlayEffectCallbackAction -> DamageSkillAction -> NextFrameSkillAction -> BuffSkillAction
//
// Original implementation:
// using System.Collections.Generic;
// using CIW.Code;
// using UnityEngine;
// using Work.YIS.Code.Buffs;
// using YIS.Code.Combat;
// using YIS.Code.Modules;
// using YIS.Code.Skills;
// using YIS.Code.Skills.Interfaces;
// using YIS.Code.Skills.Sequences;
//
// namespace PSB.Code.BattleCode.Skills.EnemySkills
// {
//     public class BattleStanceSkill : BaseSkill, IAttackSkill, IBuffOrDeBuffSkill
//     {
//         private BuffModule _buffModule;
//
//         protected override IReadOnlyList<ISkillAction> NormalSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//
//             DamageData damageData = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
//
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//             actions.Add(new DamageSkillAction(user, targets, damageData, SkillData.impulsePower));
//             actions.Add(new NextFrameSkillAction());
//             actions.Add(new BuffSkillAction(user, BuffType.ATTACK_BUFF, SkillData.damage* 2, SkillData.durationTurn));
//             //UseAttackSkill();
//
//             return actions;
//         }
//
//         protected override IReadOnlyList<ISkillAction> ChainSkillGenerateAction(Entity user, IReadOnlyList<Entity> targets)
//         {
//             List<ISkillAction> actions = new List<ISkillAction>();
//
//             DamageData damageData = new DamageData(SkillData.damage, CurrentElementalState.CurrentElemental);
//
//             actions.Add(new PlayEffectCallbackAction(this, targets));
//             actions.Add(new DamageSkillAction(user, targets, damageData, SkillData.impulsePower));
//             actions.Add(new NextFrameSkillAction());
//             actions.Add(new BuffSkillAction(user, BuffType.ATTACK_BUFF, SkillData.damage* 2, SkillData.durationTurn));
//
//             return actions;
//         }
//
//         public void UseAttackSkill()
//         {
//             var owner = GetComponentInParent<ModuleOwner>();
//             if (owner == null)
//             {
//                 Debug.LogError("DummySkill: ModuleOwner not found in parents.");
//                 return;
//             }
//
//             _buffModule = owner.GetModule<BuffModule>();
//             if (_buffModule == null)
//             {
//                 Debug.LogError($"DummySkill: BuffModule not found under owner={owner.name}");
//                 return;
//             }
//
//             _buffModule.BuffApply(BuffType.ATTACK_BUFF, SkillData.damage * 2, SkillData.durationTurn);
//         }
//
//         public void UseBuffOrDeBuffSkill()
//         {
//         }
//
//     }
// }
