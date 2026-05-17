using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Work.PSB.Code.FieldCode;

namespace PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "CompleteDetect", story: "[Sensor] complete detect", category: "Action", id: "b7bacd294613ce96dbc013fb096190db")]
    public partial class CompleteDetectAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<FieldEnemySensor> Sensor;

        protected override Status OnStart()
        {
            Sensor.Value.OnDetectConfirmed?.Invoke();
            return Status.Success;
        }
        
    }
}

