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
    [NodeDescription(name: "BattleDashToCachedPos", story: "dash [EnemyAttack] to [DashPos]", category: "Action", id: "691ff404fc102b2fd160db4388a22d95")]
    public partial class BattleDashToCachedPosAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> EnemyAttack;
        [SerializeReference] public BlackboardVariable<Vector3> DashPos;

        private Tween _t;

        protected override Status OnStart()
        {
            if (EnemyAttack?.Value == null) return Status.Failure;
            if (!EnemyAttack.Value.IsMelee) return Status.Success;

            EnemyAttack.Value.Mover.Kill();
            
            _t = EnemyAttack.Value.Mover.AnticipateAndDashTo(DashPos.Value);

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

