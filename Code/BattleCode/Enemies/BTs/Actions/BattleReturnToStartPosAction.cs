using System;
using DG.Tweening;
using PSB.Code.BattleCode.Enemies.AttackCode;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "BattleReturnToStartPos", story: "return [EnemyAttack] to [StartPos]", category: "Action", id: "ce4ba0adb806a9364767e61121d869c2")]
    public partial class BattleReturnToStartPosAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> EnemyAttack;
        [SerializeReference] public BlackboardVariable<Vector3> StartPos;

        private Tween _t;

        protected override Status OnStart()
        {
            if (EnemyAttack?.Value == null) return Status.Failure;
            
            if (!EnemyAttack.Value.IsMelee) return Status.Success;

            EnemyAttack.Value.Mover.Kill();
            _t = EnemyAttack.Value.Mover.ReturnTo(StartPos.Value);

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_t == null) return Status.Failure;
            return (_t.IsActive() && _t.IsPlaying()) ? Status.Running : Status.Success;
        }

        protected override void OnEnd()
        {
            _t = null;
        }
        
    }
}

