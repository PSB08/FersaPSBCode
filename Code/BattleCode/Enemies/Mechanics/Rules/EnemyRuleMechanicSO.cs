using System;
using System.Collections.Generic;
using CIW.Code;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules
{
    [CreateAssetMenu(fileName = "EnemyRuleMechanic", menuName = "SO/Enemy/Mechanics/RuleMechanic", order = 101)]
    public class EnemyRuleMechanicSO : EnemyMechanicSO
    {
        [SerializeField] private EnemyMechanicTriggerSO trigger;
        [SerializeField] private EnemyMechanicConditionSO[] conditions;
        [SerializeField] private EnemyMechanicTargetSelectorSO targetSelector;
        [SerializeField] private EnemyMechanicActionSO[] actions;
        
        public override EnemyMechanicRuntime CreateRuntime(EnemyMechanicContext context)
        {
            return new Runtime(context, this);
        }
        
        public override void Validate(EnemySO enemySO, EnemyMechanicValidationReport report)
        {
            if (trigger == null)
                report.AddError($"{name}에 Trigger가 없습니다.");
            else
                trigger.Validate(enemySO, report, name);
            
            if (actions == null || actions.Length == 0)
            {
                report.AddError($"{name}에 Action이 없습니다.");
            }
            else
            {
                for (int i = 0; i < actions.Length; i++)
                {
                    if (actions[i] == null)
                        report.AddError($"{name}의 Actions[{i}]가 비어있습니다.");
                    else
                        actions[i].Validate(enemySO, report, name);
                }
            }
            
            if (conditions == null) return;
            
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i] == null)
                    report.AddWarning($"{name}의 Conditions[{i}]가 비어있습니다.");
                else
                    conditions[i].Validate(enemySO, report, name);
            }
        }
        
        private sealed class Runtime : EnemyMechanicRuntime
        {
            private readonly EnemyRuleMechanicSO _so;
            private readonly List<EnemyMechanicConditionRuntime> _conditions = new();
            private readonly List<EnemyMechanicActionRuntime> _actions = new();
            private readonly List<Entity> _targets = new();
            
            private EnemyMechanicTriggerRuntime _trigger;
            private bool _executing;
            
            public Runtime(EnemyMechanicContext context, EnemyRuleMechanicSO so) : base(context, so)
            {
                _so = so;
            }
            
            protected override void OnAttach()
            {
                CreateConditions();
                CreateActions();
                
                if (_so.trigger != null)
                {
                    _trigger = _so.trigger.CreateRuntime(Context, ExecuteRule);
                    _trigger?.Attach();
                }
            }
            
            protected override void OnDetach()
            {
                _trigger?.Detach();
                _trigger = null;
                
                for (int i = _actions.Count - 1; i >= 0; i--)
                {
                    _actions[i]?.Detach();
                }
                
                for (int i = _conditions.Count - 1; i >= 0; i--)
                {
                    _conditions[i]?.Detach();
                }
                
                _actions.Clear();
                _conditions.Clear();
                _targets.Clear();
            }
            
            private void CreateConditions()
            {
                if (_so.conditions == null) return;
                
                for (int i = 0; i < _so.conditions.Length; i++)
                {
                    EnemyMechanicConditionSO conditionSO = _so.conditions[i];
                    if (conditionSO == null) continue;
                    
                    EnemyMechanicConditionRuntime condition = conditionSO.CreateRuntime(Context);
                    if (condition == null) continue;
                    
                    _conditions.Add(condition);
                    condition.Attach();
                }
            }
            
            private void CreateActions()
            {
                if (_so.actions == null) return;
                
                for (int i = 0; i < _so.actions.Length; i++)
                {
                    EnemyMechanicActionSO actionSO = _so.actions[i];
                    if (actionSO == null) continue;
                    
                    EnemyMechanicActionRuntime action = actionSO.CreateRuntime(Context);
                    if (action == null) continue;
                    
                    _actions.Add(action);
                    action.Attach();
                }
            }
            
            private void ExecuteRule(object signal)
            {
                if (_executing || !IsAttached)
                    return;
                
                _executing = true;
                
                try
                {
                    EnemyMechanicExecutionContext executionContext =
                        new EnemyMechanicExecutionContext(Context, _so, signal);
                    
                    for (int i = 0; i < _conditions.Count; i++)
                    {
                        if (!_conditions[i].IsMet(executionContext))
                            return;
                    }
                    
                    _targets.Clear();
                    
                    if (_so.targetSelector != null)
                        _so.targetSelector.CollectTargets(executionContext, _targets);
                    else if (Context.Enemy != null)
                        _targets.Add(Context.Enemy);
                    
                    if (_targets.Count == 0)
                        return;
                    
                    for (int i = 0; i < _actions.Count; i++)
                    {
                        try
                        {
                            _actions[i].Execute(executionContext, _targets);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception, _so);
                        }
                    }
                }
                finally
                {
                    _executing = false;
                }
            }
        }
        
    }
}
