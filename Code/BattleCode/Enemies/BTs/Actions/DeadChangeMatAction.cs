using System;
using PSB.Code.BattleCode.Enemies;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "DeadChangeMat", story: "[Entity] dead shader change", category: "Action", id: "bd23f5c1102c74b0cc20fd48ab1eeef3")]
    public partial class DeadChangeMatAction : Action
    {
        [SerializeReference] public BlackboardVariable<EntityDeadShader> Entity;

        protected override Status OnStart()
        {
            Entity.Value.ChangeMat();
            return Status.Success;
        }
        
    }
}

