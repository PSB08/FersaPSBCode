using System;
using Code.Scripts.Enemies.Astar;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "SetDestination", story: "[Agent] navigate to [NextPos]", category: "Action", id: "9bd918350287dd8c929d32e74690cc25")]
    public partial class SetDestinationAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<PathMovement> Agent;
        [SerializeReference] public BlackboardVariable<Vector3> NextPos;

        protected override Status OnStart()
        {
            Agent.Value.SetDestination(NextPos.Value);
            return Status.Success;
        }
        
    }
}

