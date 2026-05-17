using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Work.CSH.Scripts.PlayerComponents;
using Object = UnityEngine.Object;

namespace PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "SetPlayer", story: "[Target] setting action", category: "Action", id: "e7f79e1cf4076fba0206acff8526729f")]
    public partial class SetPlayerAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<Transform> Target;

        protected override Status OnStart()
        {
            Target.Value = Object.FindAnyObjectByType<FieldPlayer>().transform;
            return Status.Running;
        }
        
    }
}

