using PSB.Code.BattleCode.Allies.AttackCode;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PrepareAllyTurn", story: "prepare enemy turn on [Attack]", category: "Action", id: "ff06befe802b78fbab03b0a245581fd9")]
    public partial class PrepareAllyTurnAction : Action
    {
        [SerializeReference] public BlackboardVariable<AllyAttack> Attack;

        protected override Status OnStart()
        {
            if (Attack?.Value == null) return Status.Failure;
            return Attack.Value.PrepareTurnForPlanning() ? Status.Success : Status.Failure;
        }
        
    }
}

