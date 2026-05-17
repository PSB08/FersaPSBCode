using System;
using Unity.Behavior;
using UnityEngine;
using Work.PSB.Code.FieldCode;

namespace Work.PSB.Code.FieldCode.BTs.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsTargetDetecting", story: "Target detecting in [Sensor]", category: "Conditions", id: "a4ea6bea958ab9ada928192c0368b186")]
    public partial class IsTargetDetectingCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<FieldEnemySensor> Sensor;

        public override bool IsTrue()
        {
            return Sensor.Value != null && Sensor.Value.IsDetecting;
        }
        
    }
}
