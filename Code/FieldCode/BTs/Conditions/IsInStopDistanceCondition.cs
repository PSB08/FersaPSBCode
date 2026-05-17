using System;
using Unity.Behavior;
using UnityEngine;
using Work.PSB.Code.FieldCode;

namespace PSB.Code.FieldCode.BTs.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsInStopDistance", story: "Target within stop distance [Sensor] [StopDistance]", category: "Conditions", id: "c9d0f53c4728911ca2b678b2759ecb85")]
    public partial class IsInStopDistanceCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<FieldEnemySensor> Sensor;
        [SerializeReference] public BlackboardVariable<float> StopDistance;

        public override bool IsTrue()
        {
            FieldEnemySensor s = Sensor.Value;
            if (s == null || s.CurrentTarget == null) return false;

            float d = Vector2.Distance(s.transform.position, s.CurrentTarget.position);
            return d <= StopDistance.Value;
        }
        
    }
}
