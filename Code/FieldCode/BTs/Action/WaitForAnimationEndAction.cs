using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Work.PSB.Code.FieldCode.BTs;

namespace Work.PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "WaitForAnimationEnd", story: "wait for animation end in [Combat]", category: "Action", id: "2826c0f5da5c37c4a3b1702a6a383448")]
    public partial class WaitForAnimationEndAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<CombatAnimationContext> Combat;

        protected override Status OnStart()
        {
            if (Combat.Value == null)
                return Status.Failure;

            return Combat.Value.EndTriggered ? Status.Success : Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Combat.Value == null)
                return Status.Failure;

            return Combat.Value.EndTriggered ? Status.Success : Status.Running;
        }
        
    }
}

