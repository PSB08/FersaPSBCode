using PSB.Code.BattleCode.Allies;
using PSB.Code.BattleCode.Allies.AttackCode;
using PSB.Code.BattleCode.Allies.BTs.Events;
using PSW.Code.EventBus;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "CacheAlly", story: "cache start pos ro [StartPos] and dash to [DashPos] [AllyAttack] and [Target]", category: "Action", id: "8106855ee96174372138e216786f41af")]
    public partial class CacheAllyAction : Action
    {
        [SerializeReference] public BlackboardVariable<Vector3> StartPos;
        [SerializeReference] public BlackboardVariable<Vector3> DashPos;
        [SerializeReference] public BlackboardVariable<AllyAttack> AllyAttack;
        [SerializeReference] public BlackboardVariable<Transform> Target;

        protected override Status OnStart()
        {
            if (AllyAttack?.Value == null) return Status.Failure;

            if (Target != null)
                Target.Value = null;

            if (AllyAttack.Value.TrySelectTargetTransform(out Transform selectedTarget, out string reason))
            {
                Target.Value = selectedTarget;
                AllyAttack.Value.LogAiDebug($"이동 전 타겟 갱신 : {selectedTarget.name}, 이유 = {reason}");
            }

            if (Target?.Value == null)
                return EndTurnWithoutTarget(reason);

            if (!AllyAttack.Value.HasCachedTurnStartPosition)
            {
                StartPos.Value = AllyAttack.Value.Mover.Position;
                AllyAttack.Value.MarkTurnStartPositionCached();
            }

            DashPos.Value = AllyAttack.Value.Mover.GetForwardDashPos(
                StartPos.Value,
                Target.Value.position,
                AllyAttack.Value.ForwardDashDistance);

            AllyAttack.Value.FaceTarget(Target.Value);
            return Status.Success;
        }

        private Status EndTurnWithoutTarget(string reason)
        {
            AllyAttack attack = AllyAttack?.Value;
            if (attack == null)
                return Status.Failure;

            if (string.IsNullOrEmpty(reason))
                reason = "유효한 적 대상이 없습니다.";

            attack.LogAiDebug($"공격 대상 없음 : {reason}. 동료 행동을 종료합니다.");
            attack.EndAllyTurnPlanning();

            BattleAlly ally = attack.BattleAlly as BattleAlly;
            if (ally != null)
            {
                ally.SendBTState(BattleAllyState.Idle);
                Bus<AllyTurnDoneEvent>.Raise(new AllyTurnDoneEvent(ally));
            }

            return Status.Failure;
        }
        
    }
}
