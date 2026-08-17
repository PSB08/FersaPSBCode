using PSB.Code.BattleCode.Allies;
using PSB.Code.BattleCode.Entities;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "SelfDestroyAlly", story: "[Self] Destroy execute", category: "Action", id: "f234be29eeedd5798ad44bee96d48ad6")]
    public partial class SelfDestroyAllyAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleAlly> Self;

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

