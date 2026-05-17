using System;
using PSB.Code.BattleCode.Enemies.AttackCode;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "CacheAttackContext", story: "cache start pos ro [StartPos] and dash to [DashPos] [EnemyAttack] and [Target]", category: "Action", id: "8bc880d65e04e39a2ccff5f037d4c874")]
    public partial class CacheAttackContextAction : Action
    {
        [SerializeReference] public BlackboardVariable<Vector3> StartPos;
        [SerializeReference] public BlackboardVariable<Vector3> DashPos;
        [SerializeReference] public BlackboardVariable<EnemyAttack> EnemyAttack;
        [SerializeReference] public BlackboardVariable<Transform> Target;

        protected override Status OnStart()
        {
            if (EnemyAttack?.Value == null || Target?.Value == null) return Status.Failure;

            StartPos.Value = EnemyAttack.Value.Mover.Position;
            Vector3 targetPos = new Vector3(StartPos.Value.x - 0.75f,
                StartPos.Value.y, StartPos.Value.z);
            DashPos.Value = EnemyAttack.Value.Mover.GetDashTargetPos(targetPos);

            return Status.Success;
        }
        
    }
}

