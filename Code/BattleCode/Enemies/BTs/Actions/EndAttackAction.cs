using System;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.BTs.Events;
using PSB.Code.BattleCode.Skills;
using PSW.Code.EventBus;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "EndAttack", story: "raise turn done for [Self] using [Subgraph] in [SkillIndex]", category: "Action", id: "c90129137c3ae1e9333d3208dc8c853d")]
    public partial class EndAttackAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleEnemy> Self;
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
                EnemyAttack attack = Self.Value.GetModule<EnemyAttack>();
                if (attack?.SkillExecutor is ISkillUseResultExecutor resultExecutor &&
                    resultExecutor.LastUseResult.ShouldStopRemainingActions())
                {
                    attack.LogAiDebug($"추가 행동 중단 : 마지막 실행 결과 = {resultExecutor.LastUseResult}");
                    attack.EndEnemyTurnPlanning();

                    Bus<EnemyTurnDoneEvent>.Raise(new EnemyTurnDoneEvent(Self.Value));
                    _sent = true;
                    return Status.Success;
                }

                if (attack != null && EnemyTurnSubgraphRunner.TryPlan(Subgraph?.Value, attack, false, out int plannedIndex))
                {
                    SkillIndex.Value = plannedIndex;
                    attack.LogAiDebug($"추가 행동 있음 : 다음 행동 = {attack.BuildPlannedDebugText()}, 이동 상태로 재진입");
                    
                    Self.Value.SendBTState(BattleEnemyState.Move);
                    
                    _sent = true;
                    return Status.Failure;
                }

                attack?.LogAiDebug("추가 행동 없음 : 적 턴 종료 이벤트 전송");
                attack?.EndEnemyTurnPlanning();
                
                Bus<EnemyTurnDoneEvent>.Raise(new EnemyTurnDoneEvent(Self.Value));
                _sent = true;
            }

            return Status.Success;
        }
        
    }
}
