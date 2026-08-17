using DG.Tweening;
using PSB.Code.BattleCode.Allies.AttackCode;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "AllyReturnToStartPos", story: "return [AllyAttack] to [StartPos]", category: "Action", id: "e2c899c9a57ebfb54f3234749865dec4")]
    public partial class AllyReturnToStartPosAction : Action
    {
        [SerializeReference] public BlackboardVariable<AllyAttack> AllyAttack;
        [SerializeReference] public BlackboardVariable<Vector3> StartPos;

        private Tween _t;

        protected override Status OnStart()
        {
            if (AllyAttack?.Value == null) return Status.Failure;
            
            if (!AllyAttack.Value.IsMelee) return Status.Success;

            AllyAttack.Value.Mover.Kill();
            _t = AllyAttack.Value.Mover.ReturnTo(StartPos.Value);

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

