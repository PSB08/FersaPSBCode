using System;
using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.UIs;
using PSW.Code.EventBus;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Phases
{
    [CreateAssetMenu(fileName = "EnemyPhaseMechanic", menuName = "SO/Enemy/Mechanics/PhaseMechanic", order = 100)]
    public class EnemyPhaseMechanicSO : EnemyMechanicSO
    {
        [SerializeField] private EnemyPhaseDefinition[] phases;
        
        public override EnemyMechanicRuntime CreateRuntime(EnemyMechanicContext context)
        {
            return new Runtime(context, this);
        }
        
        public override void Validate(EnemySO enemySO, EnemyMechanicValidationReport report)
        {
            if (phases == null || phases.Length == 0)
            {
                report.AddError($"{name}에 페이즈가 설정되지 않았습니다.");
                return;
            }
            
            HashSet<float> thresholds = new HashSet<float>();
            
            for (int i = 0; i < phases.Length; i++)
            {
                EnemyPhaseDefinition phase = phases[i];
                
                if (phase == null)
                {
                    report.AddError($"{name}의 Phases[{i}]가 비어있습니다.");
                    continue;
                }
                
                if (phase.hpThresholdPercent <= 0f || phase.hpThresholdPercent >= 1f)
                    report.AddError($"{name}의 {i + 1}번째 페이즈 체력 기준이 올바르지 않습니다.");
                
                if (!thresholds.Add(phase.hpThresholdPercent))
                    report.AddError($"{name}에 {phase.hpThresholdPercent:P0} 체력 기준이 중복되어 있습니다.");
                
                if (phase.actions == null || phase.actions.Length == 0)
                {
                    report.AddWarning($"{name}의 {i + 1}번째 페이즈에 실행할 Action이 없습니다.");
                    continue;
                }
                
                for (int j = 0; j < phase.actions.Length; j++)
                {
                    if (phase.actions[j] == null)
                        report.AddError($"{name}의 Phases[{i}].Actions[{j}]가 비어있습니다.");
                    else
                        phase.actions[j].Validate(enemySO, report, name);
                }
            }
        }
        
        private sealed class Runtime : EnemyMechanicRuntime
        {
            private sealed class PhaseEntry
            {
                public EnemyPhaseDefinition Definition;
                public readonly List<EnemyMechanicActionRuntime> Actions = new();
            }
            
            private readonly EnemyPhaseMechanicSO _so;
            private readonly List<PhaseEntry> _entries = new();
            private readonly List<Entity> _selfTarget = new();
            
            private int _nextPhaseIndex;
            private bool _transitionPending;
            private bool _initialUiRaised;
            
            public Runtime(EnemyMechanicContext context, EnemyPhaseMechanicSO so) : base(context, so)
            {
                _so = so;
            }
            
            protected override void OnAttach()
            {
                BuildEntries();
                
                _selfTarget.Clear();
                
                if (Context.Enemy != null)
                    _selfTarget.Add(Context.Enemy);
                
                Listen<EnemyHealthChangedSignal>(HandleHealthChanged);
                Listen<EnemyBattleStartedSignal>(HandleBattleStarted);
                
                ApplyHealthThresholds();
            }
            
            protected override void OnDetach()
            {
                for (int i = _entries.Count - 1; i >= 0; i--)
                {
                    List<EnemyMechanicActionRuntime> actions = _entries[i].Actions;
                    
                    for (int j = actions.Count - 1; j >= 0; j--)
                    {
                        actions[j]?.Detach();
                    }
                }
                
                _entries.Clear();
                _selfTarget.Clear();
                _transitionPending = false;
            }
            
            private void BuildEntries()
            {
                _entries.Clear();
                
                if (_so.phases == null)
                    return;
                
                for (int i = 0; i < _so.phases.Length; i++)
                {
                    EnemyPhaseDefinition definition = _so.phases[i];
                    if (definition == null) continue;
                    
                    PhaseEntry entry = new PhaseEntry
                    {
                        Definition = definition
                    };
                    
                    if (definition.actions != null)
                    {
                        for (int j = 0; j < definition.actions.Length; j++)
                        {
                            EnemyMechanicActionSO actionSO = definition.actions[j];
                            if (actionSO == null) continue;
                            
                            try
                            {
                                EnemyMechanicActionRuntime action = actionSO.CreateRuntime(Context);
                                
                                if (action != null)
                                {
                                    entry.Actions.Add(action);
                                    action.Attach();
                                }
                            }
                            catch (Exception exception)
                            {
                                Debug.LogException(exception, actionSO);
                            }
                        }
                    }
                    
                    _entries.Add(entry);
                }
                
                _entries.Sort((left, right) =>
                    right.Definition.hpThresholdPercent.CompareTo(
                        left.Definition.hpThresholdPercent));
            }
            
            private void ApplyHealthThresholds()
            {
                if (Context.Health == null || _entries.Count == 0)
                    return;
                
                List<float> thresholds = new List<float>();
                
                for (int i = 0; i < _entries.Count; i++)
                {
                    thresholds.Add(_entries[i].Definition.hpThresholdPercent);
                }
                
                Context.Health.SetPhaseThresholds(thresholds);
            }
            
            private void HandleBattleStarted(EnemyBattleStartedSignal signal)
            {
                if (!ReferenceEquals(signal.Enemy, Context.Enemy) || _initialUiRaised)
                    return;
                
                _initialUiRaised = true;
                
                if (_entries.Count > 0)
                {
                    Bus<EnemyPhaseChangedEvent>.Raise(
                        new EnemyPhaseChangedEvent(Context.Enemy, 1, _entries.Count));
                }
            }
            
            private void HandleHealthChanged(EnemyHealthChangedSignal signal)
            {
                if (!ReferenceEquals(signal.Enemy, Context.Enemy))
                    return;
                
                TryAdvance(signal.CurrentHealth, signal.MaxHealth);
            }
            
            private void TryAdvance(float currentHealth, float maxHealth)
            {
                if (!IsAttached || _transitionPending || maxHealth <= 0f)
                    return;
                
                if (Context.Enemy == null || Context.Enemy.IsDead)
                    return;
                
                if (_nextPhaseIndex >= _entries.Count)
                    return;
                
                float healthRatio = currentHealth / maxHealth;
                PhaseEntry entry = _entries[_nextPhaseIndex];
                
                if (healthRatio > entry.Definition.hpThresholdPercent)
                    return;
                
                _transitionPending = true;
                
                EnemyPhaseTransitionRequest request = 
                    new EnemyPhaseTransitionRequest(Context, entry.Definition, _nextPhaseIndex + 1,
                    _nextPhaseIndex + 2, _entries.Count + 1);
                
                Context.PhaseTransitions.RequestTransition(request,
                    () => CompleteTransition(entry, request));
            }
            
            private void CompleteTransition(PhaseEntry entry, EnemyPhaseTransitionRequest request)
            {
                if (!IsAttached)
                    return;
                
                EnemyMechanicExecutionContext executionContext =
                    new EnemyMechanicExecutionContext(Context, _so, request);
                
                for (int i = 0; i < entry.Actions.Count; i++)
                {
                    try
                    {
                        entry.Actions[i]?.Execute(executionContext, _selfTarget);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, _so);
                    }
                }
                
                _nextPhaseIndex++;
                _transitionPending = false;
                
                Publish(new EnemyPhaseEnteredSignal(Context.Enemy, entry.Definition,
                    _nextPhaseIndex + 1, _entries.Count + 1));
                
                Bus<EnemyPhaseChangedEvent>.Raise(new EnemyPhaseChangedEvent(
                    Context.Enemy, _nextPhaseIndex + 1, _entries.Count));
                
                RaisePhaseLog(entry.Definition);
            }
            
            private void RaisePhaseLog(EnemyPhaseDefinition phase)
            {
                string enemyName = SystemLogNameResolver.GetTargetName(
                    Context.Enemy, SystemLogOwner.Enemy, "적");
                
                string phaseName = !string.IsNullOrEmpty(phase.phaseName)
                    ? phase.phaseName : $"{_nextPhaseIndex + 1}페이즈";
                
                Bus<SystemLogEvent>.Raise(new SystemLogEvent(
                    $"{enemyName}이(가) {phaseName}에 진입했습니다.", SystemLogOwner.Enemy));
            }
        }
        
    }
}
