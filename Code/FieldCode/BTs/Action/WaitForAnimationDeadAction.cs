using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Work.PSB.Code.FieldCode.BTs;

namespace Work.PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "WaitForAnimationDead", story: "wait for animation dead in [Combat]", category: "Action", id: "54317777ccdde87647f0e902faa18fd6")]
    public partial class WaitForAnimationDeadAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<CombatAnimationContext> Combat;

        protected override Status OnStart()
        {
            if (Combat.Value == null)
                return Status.Failure;

            return Combat.Value.DeadEndTriggered ? Status.Success : Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Combat.Value == null)
                return Status.Failure;

            return Combat.Value.DeadEndTriggered ? Status.Success : Status.Running;
        }
        
    }
}

