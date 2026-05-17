using System;
using Code.Scripts.Enemies.Astar;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "StopPathAgent", story: "set [Agent] stop to [IsStop]", category: "Action", id: "990f69804c7e8d6555ca815761d08910")]
    public partial class StopPathAgentAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<PathMovement> Agent;
        [SerializeReference] public BlackboardVariable<bool> IsStop;

        protected override Status OnStart()
        {
            Agent.Value.IsStop = IsStop.Value;
            return Status.Success;
        }
        
    }
}

