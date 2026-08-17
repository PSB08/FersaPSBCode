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
    [NodeDescription(name: "AllyDashToCachedPos", story: "dash [AllyAttack] to [DashPos]", category: "Action", id: "e09a3dc445556e924e67047185926d60")]
    public partial class AllyDashToCachedPosAction : Action
    {
        [SerializeReference] public BlackboardVariable<AllyAttack> AllyAttack;
        [SerializeReference] public BlackboardVariable<Vector3> DashPos;
        
        private Tween _t;
        
        protected override Status OnStart()
        {
            if (AllyAttack?.Value == null) return Status.Failure;
            if (!AllyAttack.Value.IsMelee) return Status.Success;
            if (AllyAttack.Value.HasPerformedTurnApproach) return Status.Success;
            
            AllyAttack.Value.Mover.Kill();
            
            _t = AllyAttack.Value.Mover.AnticipateAndDashTo(DashPos.Value);
            if (_t == null) return Status.Failure;
            
            AllyAttack.Value.MarkTurnApproachPerformed();
            
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

