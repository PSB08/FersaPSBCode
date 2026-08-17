using System;
using PSB.Code.BattleCode.Enemies.AttackCode;
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
        [SerializeReference] public BlackboardVariable<Transform> Target;

        protected override Status OnStart()
        {
            if (Self?.Value == null)
                return Status.Failure;

            PlayerManager.Value = Self.Value.PlayerManager;
            
            if (PlayerManager?.Value == null)
                return Status.Failure;

            EnemyAttack attack = Self.Value.GetModule<EnemyAttack>();
            if (attack != null && attack.TrySelectTargetTransform(out Transform selectedTarget, out string reason))
            {
                Target.Value = selectedTarget;
                attack.LogAiDebug($"초기 타겟 선택 : {selectedTarget.name}, 이유 = {reason}");
                return Status.Success;
            }

            Target.Value = PlayerManager.Value.BattlePlayer != null
                ? PlayerManager.Value.BattlePlayer.transform
                : null;

            return Target.Value != null ? Status.Success : Status.Failure;
        }
        
    }
}
