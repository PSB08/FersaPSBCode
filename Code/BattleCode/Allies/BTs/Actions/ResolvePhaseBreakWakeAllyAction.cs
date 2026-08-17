using PSB.Code.BattleCode.Allies;
using PSB.Code.BattleCode.Enemies.PhaseBreak;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "ResolvePhaseBreakWakeAlly", story: "resolve phase wake for [Self]", category: "Action", id: "68265b87d1b755b14b3bc8c6bd57fa93")]
    public partial class ResolvePhaseBreakWakeAllyAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleAlly> Self;

        private EnemyPhaseBreakController _ctrl;

        protected override Status OnStart()
        {
            _ctrl = GetController();
            if (_ctrl == null)
            {
                return Status.Failure;
            }
            
            _ctrl.StartWakeFromBT();
            return _ctrl.WakeDone ? DoneStatus() : Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_ctrl == null)
            {
                return Status.Failure;
            }

            return _ctrl.WakeDone ? DoneStatus() : Status.Running;
        }

        private Status DoneStatus()
        {
            return _ctrl.WakeOk ? Status.Success : Status.Failure;
        }

        private EnemyPhaseBreakController GetController()
        {
            return Self?.Value != null ? Self.Value.GetComponent<EnemyPhaseBreakController>() : null;
        }
        
    }
}

