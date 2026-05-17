using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "SelfDestroy", story: "[Self] Destroy execute", category: "Action", id: "cd5857f8543625467991d4b60d7859e5")]
    public partial class SelfDestroyAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleEnemy> Self;

        protected override Status OnStart()
        {
            Self.Value.DestroyEntity();
            return Status.Success;
        }
        
    }
}

