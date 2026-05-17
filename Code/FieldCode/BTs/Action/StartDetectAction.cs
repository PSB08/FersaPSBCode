using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Work.PSB.Code.FieldCode;

namespace Work.PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "StartDetect", story: "[Sensor] start detect", category: "Action", id: "0d66b95dc42a1ad651741cf9a35462dd")]
    public partial class StartDetectAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<FieldEnemySensor> Sensor;

        protected override Status OnUpdate()
        {
            if (Sensor.Value == null)
                return Status.Failure;

            if (Sensor.Value.IsPlayerInSight(out _))
                return Status.Success;
            else
                return Status.Failure;
        }
        
    }
}

