using System;
using Unity.Behavior;
using UnityEngine;
using Work.PSB.Code.FieldCode;

namespace Work.PSB.Code.FieldCode.BTs.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsTargetDetected", story: "Target detected in [Sensor]", category: "Conditions", id: "b1ff0abccb40440f16723bb56bafa712")]
    public partial class IsTargetDetectedCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<FieldEnemySensor> Sensor;

        public override bool IsTrue()
        {
            return Sensor.Value != null && Sensor.Value.PlayerDetected;
        }
        
    }
}
