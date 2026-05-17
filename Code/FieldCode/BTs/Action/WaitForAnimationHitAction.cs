using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Work.PSB.Code.FieldCode.BTs;

namespace Work.PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "WaitForAnimationHit", story: "wait for animation hit in [Combat]", category: "Action", id: "3409cb9c0c2edcb8657564d92aa6eee3")]
    public partial class WaitForAnimationHitAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<CombatAnimationContext> Combat;

        protected override Status OnStart()
        {
            if (Combat.Value == null)
                return Status.Failure;

            return Combat.Value.HitTriggered ? Status.Success : Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Combat.Value == null)
                return Status.Failure;

            return Combat.Value.HitTriggered ? Status.Success : Status.Running;
        }
        
    }
}

