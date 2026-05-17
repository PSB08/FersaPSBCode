using System;
using PSB.Code.BattleCode.Enemies.BTs.Events;
using PSW.Code.EventBus;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "EndAttack", story: "raise turn done for [Self]", category: "Action", id: "c90129137c3ae1e9333d3208dc8c853d")]
    public partial class EndAttackAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleEnemy> Self;

        protected override Status OnStart()
        {
            Bus<EnemyTurnDoneEvent>.Raise(new EnemyTurnDoneEvent(Self));
            return Status.Success;
        }
        
    }
}

