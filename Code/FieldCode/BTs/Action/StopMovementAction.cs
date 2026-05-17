using System;
using Code.Scripts.Enemies.BT;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "StopMovement", story: "stop [Movement]", category: "Action", id: "4bd8529cb5db0a36304766f4fbc56538")]
    public partial class StopMovementAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<AgentMovement> Movement;

        protected override Status OnStart()
        {
            Movement.Value.StopImmediately();
            return Status.Success;
        }
        
    }
}

