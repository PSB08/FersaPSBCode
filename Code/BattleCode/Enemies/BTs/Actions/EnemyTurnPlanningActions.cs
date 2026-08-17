using System;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Enemies.AttackCode;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Object = UnityEngine.Object;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "RunEnemyTurnSubgraph", story: "run [Subgraph] for [Attack] in [SkillIndex]", category: "Action", id: "8104b35a4ce84b52a985114b1b8db38a")]
    public partial class RunEnemyTurnSubgraphAction : Action
    {
        [SerializeReference] public BlackboardVariable<BehaviorGraph> Subgraph;
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;
        
        protected override Status OnStart()
        {
            if (Attack?.Value == null) return Status.Failure;

            bool ok = EnemyTurnSubgraphRunner.TryPlan(Subgraph?.Value, Attack.Value, true, out int index);
            SkillIndex.Value = ok ? index : -1;
            return ok ? Status.Success : Status.Failure;
        }
    }
    
    public static class EnemyTurnSubgraphRunner
    {
        private static readonly BattleEnemyTurnState[] Steps =
        {
            BattleEnemyTurnState.DecideDefense,
            BattleEnemyTurnState.PlanAction,
            BattleEnemyTurnState.SelectBestAction
        };
        
        public static bool TryPlan(BehaviorGraph source, EnemyAttack attack, bool allowSkip, out int plannedIndex)
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
                RunState(graph, attack, BattleEnemyTurnState.PrepareTurn, ref plannedIndex);
                
                for (int i = 0; i < Steps.Length; i++)
                {
                    BattleEnemyTurnState state = Steps[i];
                    RunState(graph, attack, state, ref plannedIndex);
                    
                    if (TryFinish(attack, state, allowSkip, out plannedIndex))
                        return true;
                }
                
                if (allowSkip)
                {
                    RunState(graph, attack, BattleEnemyTurnState.SkipTurn, ref plannedIndex);
                    if (TryFinish(attack, BattleEnemyTurnState.SkipTurn, true, out plannedIndex))
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
        
        private static void RunState(BehaviorGraph graph, EnemyAttack attack, BattleEnemyTurnState state, ref int plannedIndex)
        {
            if (graph == null)
                return;
            
            SetVariable(graph, "Attack", attack);
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
        
        private static bool TryFinish(EnemyAttack attack, BattleEnemyTurnState state, bool allowSkip, out int plannedIndex)
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
    
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PrepareEnemyTurn", story: "prepare enemy turn on [Attack]", category: "Action", id: "d264f932c8cc4a0aa38749d04efe01fd")]
    public partial class PrepareEnemyTurnAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;
        
        protected override Status OnStart()
        {
            if (Attack?.Value == null) return Status.Failure;
            return Attack.Value.PrepareTurnForPlanning() ? Status.Success : Status.Failure;
        }
    }
    
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlanDefenseIntent", story: "plan defense intent on [Attack] in [SkillIndex]", category: "Action", id: "cb3ce78f2d564ea5b8db6bde7657c35e")]
    public partial class PlanDefenseIntentAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;
        
        protected override Status OnStart()
        {
            return EnemyPlanActionResult.Finish(Attack?.Value,
                Attack?.Value != null && Attack.Value.TryPlanDefenseIntent(), SkillIndex);
        }
    }
    
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlanDrawIntent", story: "plan draw intent on [Attack] in [SkillIndex]", category: "Action", id: "379e66bf95a845f1a401fb6558488749")]
    public partial class PlanDrawIntentAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;
        
        protected override Status OnStart()
        {
            return EnemyPlanActionResult.Finish(Attack?.Value,
                Attack?.Value != null && Attack.Value.TryPlanDrawIntent(), SkillIndex);
        }
    }
    
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlanCostRecoveryIntent", story: "plan cost recovery intent on [Attack] in [SkillIndex]", category: "Action", id: "4e9dca7bcfac43a1b5164e4d587270f2")]
    public partial class PlanCostRecoveryIntentAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;
        
        protected override Status OnStart()
        {
            return EnemyPlanActionResult.Finish(Attack?.Value,
                Attack?.Value != null && Attack.Value.TryPlanCostRecoveryIntent(), SkillIndex);
        }
    }
    
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlanPatternIntent", story: "plan pattern intent on [Attack] in [SkillIndex]", category: "Action", id: "26edbe89d5334f32a92cde1f65eff3bc")]
    public partial class PlanPatternIntentAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;
        
        protected override Status OnStart()
        {
            return EnemyPlanActionResult.Finish(Attack?.Value,
                Attack?.Value != null && Attack.Value.TryPlanPatternIntent(), SkillIndex);
        }
    }
    
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlanBestActionIntent", story: "plan best action intent on [Attack] in [SkillIndex]", category: "Action", id: "1c23c845d8c44af0abc00cb50c33eef3")]
    public partial class PlanBestActionIntentAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;
        
        protected override Status OnStart()
        {
            return EnemyPlanActionResult.Finish(Attack?.Value,
                Attack?.Value != null && Attack.Value.TryPlanBestActionIntent(), SkillIndex);
        }
    }
    
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlanSkipIntent", story: "plan skip intent on [Attack] in [SkillIndex]", category: "Action", id: "0bffec01779047d998016f3dd47b569d")]
    public partial class PlanSkipIntentAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;
        
        protected override Status OnStart()
        {
            return EnemyPlanActionResult.Finish(Attack?.Value,
                Attack?.Value != null && Attack.Value.PlanSkipIntent(), SkillIndex);
        }
    }
    
    public static class EnemyPlanActionResult
    {
        public static Action.Status Finish(EnemyAttack attack, bool success, BlackboardVariable<int> skillIndex)
        {
            if (skillIndex != null)
                skillIndex.Value = success && attack != null ? attack.PlannedIndex : -1;

            return success ? Action.Status.Success : Action.Status.Failure;
        }
    }
    
}
