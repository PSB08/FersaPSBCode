using System;
using PSB.Code.BattleCode.Enemies.AttackCode;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PickRandomSkillIndex", story: "pick random skill index from [Attack] to [SkillIndex]", category: "Action", id: "1366c604c62941b30b5525e0934d5ab9")]
    public partial class PickRandomSkillIndexAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;

        protected override Status OnStart()
        {
            if (Attack?.Value == null) return Status.Failure;

            int idx = Attack.Value.SkillExecutor != null
                ? Attack.Value.SkillExecutor.PickRandomSkillIndex()
                : -1;

            SkillIndex.Value = idx;

            return (idx >= 0) ? Status.Success : Status.Failure;
        }
        
    }
}

