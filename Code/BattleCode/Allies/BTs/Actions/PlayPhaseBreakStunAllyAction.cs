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
    [NodeDescription(name: "PlayPhaseBreakStunAlly", story: "phase break stun for [Self]", category: "Action", id: "b5c2bcbdbc7900b4a5f6f20ad31e9c7d")]
    public partial class PlayPhaseBreakStunAllyAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleAlly> Self;

        protected override Status OnStart()
        {
            EnemyPhaseBreakController controller = GetController();
            if (controller == null)
            {
                return Status.Failure;
            }

            controller.PlayStunFromBT();
            return Status.Success;
        }

        private EnemyPhaseBreakController GetController()
        {
            return Self?.Value != null ? Self.Value.GetComponent<EnemyPhaseBreakController>() : null;
        }
        
    }
}

