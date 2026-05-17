using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "DeadTriggerOn", story: "[Self] dead trigger", category: "Action", id: "3f256e9f33c948c80a92442dc67c3741")]
    public partial class DeadTriggerOnAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleEnemy> Self;

        protected override Status OnStart()
        {
            Self.Value.IsDead = true;
            return Status.Success;
        }

        
    }
}

