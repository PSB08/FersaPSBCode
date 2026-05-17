using System;
using Code.Scripts.Enemies.Astar;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "WaitForArrive", story: "[Agent] wait for arrive", category: "Action", id: "8018c8cd7f0d9e67faf573a143e544eb")]
    public partial class WaitForArriveAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<PathMovement> Agent;

        protected override Status OnUpdate()
        {
            if(Agent.Value.IsArrived)
                return Status.Success;
            if(Agent.Value.IsPathFailed)
                return Status.Failure;
        
            return Status.Running;
        }
        
    }
}

