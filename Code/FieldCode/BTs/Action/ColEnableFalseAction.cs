using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Work.PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ColEnableFalse", story: "[Collider] enabled [Enabled]", category: "Action", id: "29c4b1046a39c84a5b6243dc97cdfb1f")]
    public partial class ColEnableFalseAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<Collider2D> Collider;
        [SerializeReference] public BlackboardVariable<bool> Enabled;

        protected override Status OnStart()
        {
            Collider.Value.enabled = Enabled;
            return Status.Success;
        }

    }
}

