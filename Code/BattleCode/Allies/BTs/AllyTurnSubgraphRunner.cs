using PSB.Code.BattleCode.Allies.AttackCode;
using Unity.Behavior;
using UnityEngine;

namespace PSB.Code.BattleCode.Allies.BTs
{
    public static class AllyTurnSubgraphRunner
    {
        private static readonly BattleAllyTurnState[] Steps =
        {
            BattleAllyTurnState.DecideDefense,
            BattleAllyTurnState.SelectBestAction
        };
        
        public static bool TryPlan(BehaviorGraph source, AllyAttack attack, bool allowSkip, out int plannedIndex)
        {
            plannedIndex = -1;
            
            if (attack == null)
                return false;
            
            if (source == null)
            {
                attack.LogAiDebug("턴 서브 그래프가 없어 코드 계획으로 대체합니다.");
                bool fallback = allowSkip ? attack.PlanNextIntent() : attack.TryPlanNextActionInCurrentTurn();
                plannedIndex = fallback ? attack.PlannedIndex : -1;
                return fallback;
            }
            
            BehaviorGraph graph = Object.Instantiate(source);
            
            try
            {
                attack.LogAiDebug($"턴 서브 그래프 실행 시작 : {source.name}");
                RunState(graph, attack, BattleAllyTurnState.PrepareTurn, ref plannedIndex);
                
                for (int i = 0; i < Steps.Length; i++)
                {
                    BattleAllyTurnState state = Steps[i];
                    RunState(graph, attack, state, ref plannedIndex);
                    
                    if (TryFinish(attack, state, allowSkip, out plannedIndex))
                        return true;
                }
                
                if (allowSkip)
                {
                    RunState(graph, attack, BattleAllyTurnState.SkipTurn, ref plannedIndex);
                    if (TryFinish(attack, BattleAllyTurnState.SkipTurn, true, out plannedIndex))
                        return true;
                }
                
                attack.LogAiDebug("턴 서브 그래프 결과 : 선택 가능한 행동이 없습니다.");
                return false;
            }
            finally
            {
                DestroyGraph(graph);
            }
        }
        
        private static void RunState(BehaviorGraph graph, AllyAttack attack, BattleAllyTurnState state, ref int plannedIndex)
        {
            if (graph == null) return;
            
            SetVariable(graph, "Attack", attack);
            SetVariable(graph, "AllyAttack", attack);
            SetVariable(graph, "SkillIndex", -1);
            SetVariable(graph, "TurnState", state);
            
            attack.LogAiDebug($"턴 서브 그래프 상태 실행 : {state}");
            
            graph.Start();
            
            const int maxTicks = 8;
            for (int i = 0; i < maxTicks && graph.IsRunning; i++)
                graph.Tick();
            
            PullSkillIndex(graph, ref plannedIndex);
            graph.End();
        }
        
        private static bool TryFinish(AllyAttack attack, BattleAllyTurnState state, bool allowSkip, out int plannedIndex)
        {
            if (attack.HasPlanned)
            {
                plannedIndex = attack.PlannedIndex;
                attack.LogAiDebug($"턴 서브 그래프 선택 완료 : 상태 = {state}, 선택 번호 = {plannedIndex}");
                return true;
            }
            
            if (allowSkip && attack.HasPlannedSkip)
            {
                plannedIndex = -1;
                attack.LogAiDebug($"턴 서브 그래프 선택 완료 : 상태 = {state}, 스킵");
                return true;
            }
            
            plannedIndex = -1;
            return false;
        }
        
        private static void PullSkillIndex(BehaviorGraph graph, ref int plannedIndex)
        {
            if (graph == null) return;
            if (!graph.BlackboardReference.GetVariable("SkillIndex", out BlackboardVariable variable)) return;
            if (variable.ObjectValue is int value)
                plannedIndex = value;
        }
        
        private static void SetVariable(BehaviorGraph graph, string name, object value)
        {
            if (graph == null) return;
            if (!graph.BlackboardReference.GetVariable(name, out BlackboardVariable variable)) return;
            variable.ObjectValue = value;
        }
        
        private static void DestroyGraph(BehaviorGraph graph)
        {
            if (graph == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(graph);
                return;
            }
#endif
            Object.Destroy(graph);
        }
        
    }
}
