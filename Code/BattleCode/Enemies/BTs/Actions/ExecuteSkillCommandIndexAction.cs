using System;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Players;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using YIS.Code.CoreSystem;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ExecuteSkillCommandIndex", story: "execute skill command on [EnemyAttack] using [SkillIndex] and [Target]", category: "Action", id: "ad7ac2566614d48b3d8a705f70eb9334")]
    public partial class ExecuteSkillCommandIndexAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> EnemyAttack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;
        [SerializeReference] public BlackboardVariable<Transform> Target;

        [SerializeReference] public BlackboardVariable<bool> AttackResult;

        protected override Status OnStart()
        {
            AttackResult.Value = false;

            if (EnemyAttack?.Value == null) return Status.Failure;
            if (Target?.Value == null) return Status.Failure;

            var attack = EnemyAttack.Value;

            if (attack.skillCommand == null) return Status.Failure;
            if (attack.SkillExecutor == null) return Status.Failure;

            int idx = SkillIndex.Value;
            if (idx < 0) return Status.Failure;
            
            if (attack.HasPlanned)
                idx = attack.PlannedIndex;

            var enemyOwner = attack.BattleEnemy;
            if (enemyOwner == null) return Status.Failure;

            var battlePlayer = Target.Value.GetComponentInParent<BattlePlayer>();
            if (battlePlayer == null) return Status.Failure;

            if (!attack.SkillExecutor.TryGetSkillIdByIndex(idx, out var id))
                return Status.Failure;

            bool isChain = false;

            Context ctx = new Context(
                canChain: isChain,
                skillIds: new[] { id },
                caster: enemyOwner,
                target: battlePlayer.transform,
                executor: attack.SkillExecutor,
                chainFlags: new[] { isChain }
            );
            
            if (!attack.skillCommand.CanHandle(ctx))
            {
                AttackResult.Value = false;
                return Status.Success;
            }
            
            AttackResult.Value = attack.skillCommand.Handle(ctx);
            return Status.Success;
        }
        
    }
}

