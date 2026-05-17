using System;
using Unity.Behavior;
using UnityEngine;
using Work.PSB.Code.FieldCode;

namespace PSB.Code.FieldCode.BTs.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "TargetInSight", story: "Target in [Sensor]", category: "Conditions", id: "e104cf1e97070c0bdf29ceb810834d8f")]
    public partial class TargetInSightCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<FieldEnemySensor> Sensor;

        public override bool IsTrue()
        {
            if (Sensor.Value == null) return false;
            return Sensor.Value.IsPlayerInSight(out _);
        }
        
    }
}
