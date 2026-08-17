using PSB.Code.BattleCode.Allies;
using PSB.Code.BattleCode.Allies.AttackCode;
using PSB.Code.BattleCode.Allies.BTs;
using PSB.Code.BattleCode.Allies.BTs.Events;
using PSB.Code.BattleCode.Skills;
using PSW.Code.EventBus;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Work.PSB.Code.RunSystem;
using Action = Unity.Behavior.Action;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "EndAttackAlly", story: "raise turn done for [Self] using [Subgraph] in [SkillIndex]", category: "Action", id: "b650068509dbaa613f63950e2ecd15a3")]
    public partial class EndAttackAllyAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleAlly> Self;
        [SerializeReference] public BlackboardVariable<BehaviorGraph> Subgraph;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;

        private int _endFrame;
        private bool _sent;

        protected override Status OnStart()
        {
            if (Self?.Value == null) return Status.Failure;

            _endFrame = Time.frameCount + 5;
            _sent = false;
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Self?.Value == null) return Status.Failure;
            if (Time.frameCount < _endFrame) return Status.Running;

            if (!_sent)
            {
                AllyAttack attack = Self.Value.GetModule<AllyAttack>();
                if (attack?.SkillExecutor is ISkillUseResultExecutor resultExecutor &&
                    resultExecutor.LastUseResult.ShouldStopRemainingActions())
                {
                    attack.LogAiDebug($"추가 행동 중단 : 마지막 실행 결과 = {resultExecutor.LastUseResult}");
                    attack.EndAllyTurnPlanning();

                    Bus<AllyTurnDoneEvent>.Raise(new AllyTurnDoneEvent(Self.Value));
                    _sent = true;
                    return Status.Success;
                }

                if (RunEventBattleHealthController.ShouldStopAdditionalActions)
                {
                    attack?.LogAiDebug("추가 행동 중단 : 이벤트 전투의 체력 퍼센트 조건을 달성했습니다.");
                    attack?.EndAllyTurnPlanning();

                    Bus<AllyTurnDoneEvent>.Raise(new AllyTurnDoneEvent(Self.Value));
                    _sent = true;
                    return Status.Success;
                }

                if (attack != null && !attack.TrySelectTargetTransform(out _, out string reason))
                {
                    attack.LogAiDebug($"추가 행동 중단 : 공격 가능한 타겟이 없습니다. 이유 = {reason}");
                    attack.EndAllyTurnPlanning();

                    Bus<AllyTurnDoneEvent>.Raise(new AllyTurnDoneEvent(Self.Value));
                    _sent = true;
                    return Status.Success;
                }
                
                if (attack != null && AllyTurnSubgraphRunner.TryPlan(Subgraph?.Value, attack, false, out int plannedIndex))
                {
                    SkillIndex.Value = plannedIndex;
                    attack.LogAiDebug($"추가 행동 있음 : 다음 행동 = {attack.BuildPlannedDebugText()}, 이동 상태로 재진입");

                    Self.Value.SendBTState(BattleAllyState.Move);

                    _sent = true;
                    return Status.Failure;
                }

                attack?.LogAiDebug("추가 행동 없음 : 동료 턴 종료 이벤트 전송");
                attack?.EndAllyTurnPlanning();

                Bus<AllyTurnDoneEvent>.Raise(new AllyTurnDoneEvent(Self.Value));
                _sent = true;
            }

            return Status.Success;
        }
        
    }
    
}
