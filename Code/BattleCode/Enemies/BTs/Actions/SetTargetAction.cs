using System;
using PSB.Code.BattleCode.Players;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "SetTarget", story: "Set [Target]", category: "Action", id: "c359a08dbfe0502450c9659b38754490")]
    public partial class SetTargetAction : Action
    {
        [SerializeReference] public BlackboardVariable<NormalBattleEnemy> Self;
        [SerializeReference] public BlackboardVariable<PlayerManager> PlayerManager;
        [SerializeReference] public BlackboardVariable<BattlePlayer> Target;

        protected override Status OnStart()
        {
            PlayerManager.Value = Self.Value.PlayerManager;
            
            if (PlayerManager?.Value == null)
                return Status.Failure;

            Target.Value = PlayerManager.Value.BattlePlayer;
            return Target.Value != null ? Status.Success : Status.Failure;
        }
        
    }
}

