using System;
using CIW.Code;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Players;
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
            if (EnemyAttack?.Value == null)
            {
                ClearTarget();
                return Status.Success;
            }
            
            EnemyAttack attack = EnemyAttack.Value;
            ClearTargetIfInvalid();
            
            if (attack.TrySelectTargetTransform(out Transform selectedTarget, out string reason))
            {
                Target.Value = selectedTarget;
                attack.LogAiDebug($"이동 전 타겟 갱신 : {selectedTarget.name}, 이유 = {reason}");
            }
            
            if (Target?.Value == null)
            {
                attack.PlanSkipIntent();
                attack.LogAiDebug("이동 전 타겟 없음 : 이번 행동을 스킵합니다.");
                return Status.Success;
            }
            
            if (!attack.HasCachedTurnStartPosition)
            {
                StartPos.Value = attack.Mover.Position;
                attack.MarkTurnStartPositionCached();
            }
            
            Vector3 targetPos = new Vector3(StartPos.Value.x - 0.75f, StartPos.Value.y, StartPos.Value.z);
            DashPos.Value = attack.Mover.GetDashTargetPos(targetPos);
            
            return Status.Success;
        }
        
        private void ClearTargetIfInvalid()
        {
            if (Target?.Value == null)
                return;
            
            Entity targetEntity = Target.Value.GetComponentInParent<Entity>();
            if (targetEntity != null && SkillTargetingUtil.CanBeDirectTarget(targetEntity))
                return;
            
            ClearTarget();
        }
        
        private void ClearTarget()
        {
            if (Target != null)
                Target.Value = null;
        }
        
    }
}
