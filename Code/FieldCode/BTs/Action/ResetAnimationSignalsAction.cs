using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Work.PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ResetAnimationSignals", story: "reset animation signals in [Combat]", category: "Action", id: "63139df2abc1817ede5d78cdb1b0adc9")]
    public partial class ResetAnimationSignalsAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<CombatAnimationContext> Combat;

        protected override Status OnStart()
        {
            if (Combat.Value == null)
                return Status.Failure;

            Combat.Value.ResetFlags();
            return Status.Success;
        }
        
    }
}

