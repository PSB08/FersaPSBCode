using System;
using PSB.Code.BattleCode.Enemies.AttackCode;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PreActivateSkillByIndex", story: "pre activate skill on [Attack] using [SkillIndex]", category: "Action", id: "7acf7bbd546b18b018d256c9882d53c2")]
    public partial class PreActivateSkillByIndexAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;

        protected override Status OnStart()
        {
            if (Attack?.Value == null) return Status.Failure;

            int idx = SkillIndex.Value;
            if (idx < 0) return Status.Failure;

            return Attack.Value.PreActivateByIndex(idx) ? Status.Success : Status.Failure;
        }
        
    }
}

