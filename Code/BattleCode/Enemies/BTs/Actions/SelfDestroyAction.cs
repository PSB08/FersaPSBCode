using System;
using PSB.Code.BattleCode.Entities;
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

        private EntityHealth _health;

        protected override Status OnStart()
        {
            _health = Self.Value != null ? Self.Value.GetModule<EntityHealth>() : null;
            return TryDestroy();
        }

        protected override Status OnUpdate()
        {
            return TryDestroy();
        }

        private Status TryDestroy()
        {
            if (Self.Value == null)
                return Status.Success;

            if (_health != null && !_health.CanFinishDeathSequence)
                return Status.Running;

            Self.Value.DestroyEntity();
            return Status.Success;
        }
        
    }
}

