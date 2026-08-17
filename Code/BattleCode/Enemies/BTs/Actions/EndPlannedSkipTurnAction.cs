using System;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.BTs.Events;
using PSW.Code.EventBus;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "EndPlannedSkipTurn", story: "end planned skip turn for [Self] using [Attack]", category: "Action", id: "80fefd570f06e1d06484b7c5c4951eff")]
    public partial class EndPlannedSkipTurnAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleEnemy> Self;
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;

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
                Attack?.Value?.EndEnemyTurnPlanning();
                
                Self.Value.SendBTState(BattleEnemyState.Idle);
                
                Bus<EnemyTurnDoneEvent>.Raise(new EnemyTurnDoneEvent(Self.Value));
                _sent = true;
            }

            return Status.Success;
        }
        
    }
}
