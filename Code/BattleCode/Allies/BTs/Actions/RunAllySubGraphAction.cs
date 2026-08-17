using PSB.Code.BattleCode.Allies.AttackCode;
using PSB.Code.BattleCode.Allies.BTs;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "RunAllySubGraph", story: "run [Subgraph] for [Attack] in [SkillIndex]", category: "Action", id: "cd7b927408ffa56331510ace778bb411")]
    public partial class RunAllySubGraphAction : Action
    {
        [SerializeReference] public BlackboardVariable<BehaviorGraph> Subgraph;
        [SerializeReference] public BlackboardVariable<AllyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;

        protected override Status OnStart()
        {
            if (Attack?.Value == null) return Status.Failure;

            bool ok = AllyTurnSubgraphRunner.TryPlan(Subgraph?.Value, Attack.Value, true, out int index);
            SkillIndex.Value = ok ? index : -1;
            return ok ? Status.Success : Status.Failure;
        }
        
    }
}

