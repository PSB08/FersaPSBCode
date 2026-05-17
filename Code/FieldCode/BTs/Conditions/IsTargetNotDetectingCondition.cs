using System;
using Unity.Behavior;
using UnityEngine;
using Work.PSB.Code.FieldCode;

namespace PSB.Code.FieldCode.BTs.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsTargetNotDetecting", story: "Target NOT detecting in [Sensor]", category: "Conditions", id: "b67fc646f1381034ffd14e8f1a185678")]
    public partial class IsTargetNotDetectingCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<FieldEnemySensor> Sensor;

        public override bool IsTrue()
        {
            return Sensor.Value == null || !Sensor.Value.IsDetecting;
        }
        
    }
}
