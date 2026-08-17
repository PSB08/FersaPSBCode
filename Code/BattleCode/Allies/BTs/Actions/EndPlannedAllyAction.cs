using PSB.Code.BattleCode.Allies.AttackCode;
using PSB.Code.BattleCode.Allies.BTs.Events;
using PSW.Code.EventBus;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Allies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "EndPlannedAlly", story: "end planned skip turn for [Self] using [Attack]", category: "Action", id: "90838308d6ebfe53ec0a95a1ce0760c6")]
    public partial class EndPlannedAllyAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleAlly> Self;
        [SerializeReference] public BlackboardVariable<AllyAttack> Attack;

        private int _endFrame;
        private bool _sent;

        protected override Status OnStart()
        {
            if (Self?.Value == null) return Status.Failure;

            _endFrame = Time.frameCount + 1;
            _sent = false;
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Self?.Value == null) return Status.Failure;
            if (Time.frameCount < _endFrame) return Status.Running;

            if (!_sent)
            {
                Attack?.Value?.ClearIntent();
                Attack?.Value?.EndAllyTurnPlanning();
                
                Self.Value.SendBTState(BattleAllyState.Idle);
                
                Bus<AllyTurnDoneEvent>.Raise(new AllyTurnDoneEvent(Self.Value));
                _sent = true;
            }

            return Status.Success;
        }
        
    }
}

