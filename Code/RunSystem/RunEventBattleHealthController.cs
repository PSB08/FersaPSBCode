using System.Collections;
using System;
using CIW.Code.System.Events;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Entities;
using PSW.Code.EventBus;
using UnityEngine;
using Work.CSH.Scripts.Managers;

namespace Work.PSB.Code.RunSystem
{
    public sealed class RunEventBattleHealthController : MonoBehaviour
    {
        private static RunEventBattleHealthController _activeController;

        [SerializeField, Min(0f)] private float battleEndDelay = 0.6f;
        
        private RunEventSO _eventData;
        private EntityHealth _targetHealth;
        private TurnManagerSO _turnManager;
        
        private Func<bool> _previousBattleEndedCondition;
        private Func<bool> _eventBattleEndedCondition;
        private Coroutine _battleEndCoroutine;
        
        private bool _conditionReached;
        private bool _undyingTriggered;
        private bool _battleEnded;
        
        public static bool ShouldStopAdditionalActions
        {
            get
            {
                return _activeController != null &&
                       _activeController.ShouldStopAdditionalActionsInternal();
            }
        }
        
        private bool ShouldStopAdditionalActionsInternal()
        {
            if (_eventData == null || _battleEnded)
                return false;
            
            switch (_eventData.battleHealthEndMode)
            {
                case RunEventBattleHealthEndMode.HealthOne:
                    return _conditionReached && _undyingTriggered;
                
                case RunEventBattleHealthEndMode.HealthPercent:
                    return _conditionReached;
                
                default:
                    return false;
            }
        }
        
        public void Initialize(RunEventSO eventData)
        {
            _eventData = eventData;
            _conditionReached = false;
            _undyingTriggered = false;
            _battleEnded = false;
            _activeController = this;
        }
        
        private void OnEnable()
        {
            Bus<BattleEnd>.OnEvent += HandleBattleEnd;
        }
        
        private void OnDisable()
        {
            Bus<BattleEnd>.OnEvent -= HandleBattleEnd;
        }
        
        private void Update()
        {
            if (_targetHealth != null || _eventData == null || _battleEnded)
                return;
            
            TryBindTargetEnemy();
        }
        
        private void TryBindTargetEnemy()
        {
            BattleEnemy[] enemies = FindObjectsByType<BattleEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            for (int i = 0; i < enemies.Length; i++)
            {
                BattleEnemy enemy = enemies[i];
                if (enemy == null || enemy.EncounterIndex != _eventData.battleHealthTargetEnemyIndex)
                    continue;
                
                EntityHealth health = enemy.GetModule<EntityHealth>();
                TurnManagerSO turnManager = enemy.TurnManager;
                if (health == null || turnManager == null || turnManager.OnBattleEndedCondition == null)
                    continue;
                
                BindTargetEnemy(health, turnManager);
                return;
            }
        }
        
        private void BindTargetEnemy(EntityHealth health, TurnManagerSO turnManager)
        {
            _targetHealth = health;
            _turnManager = turnManager;
            
            _targetHealth.AddUndyingModifier(this);
            _targetHealth.OnTotalHealthChangeEvent += HandleTargetHealthChanged;
            _targetHealth.OnUndyingTriggeredEvent += HandleTargetUndyingTriggered;
            
            _previousBattleEndedCondition = _turnManager.OnBattleEndedCondition;
            _eventBattleEndedCondition = CheckBattleEnded;
            _turnManager.OnBattleEndedCondition = _eventBattleEndedCondition;
            
            EvaluateHealthCondition(_targetHealth.TotalCurrentHealth, _targetHealth.TotalMaxHealth);
        }
        
        private void HandleTargetHealthChanged(float currentHealth, float maxHealth)
        {
            EvaluateHealthCondition(currentHealth, maxHealth);
        }
        
        private void HandleTargetUndyingTriggered()
        {
            if (_eventData == null || _battleEnded)
                return;
            
            if (_eventData.battleHealthEndMode != RunEventBattleHealthEndMode.HealthOne)
                return;
            
            if (!_conditionReached)
                return;
            
            _undyingTriggered = true;
        }
        
        private void EvaluateHealthCondition(float currentHealth, float maxHealth)
        {
            if (_conditionReached || _battleEnded)
                return;
            
            switch (_eventData.battleHealthEndMode)
            {
                case RunEventBattleHealthEndMode.HealthOne:
                    _conditionReached = currentHealth <= 1f;
                    break;
                
                case RunEventBattleHealthEndMode.HealthPercent:
                    _conditionReached = maxHealth > 0f && currentHealth / maxHealth <= _eventData.battleHealthTargetPercent;
                    break;
            }
        }
        
        private bool CheckBattleEnded()
        {
            if (_previousBattleEndedCondition != null && _previousBattleEndedCondition.Invoke())
                return true;
            
            if (_battleEnded || _battleEndCoroutine != null)
                return true;
            
            if (!_conditionReached)
                return false;
            
            _battleEndCoroutine = StartCoroutine(CompleteBattleAfterDelay());
            return true;
        }
        
        private IEnumerator CompleteBattleAfterDelay()
        {
            if (battleEndDelay > 0f)
                yield return new WaitForSecondsRealtime(battleEndDelay);
            
            _battleEndCoroutine = null;
            
            if (_battleEnded)
                yield break;
            
            _turnManager.OnTurnEnded?.Invoke(_turnManager.Turn);
            Bus<BattleEnd>.Raise(new BattleEnd(true));
        }
        
        private void HandleBattleEnd(BattleEnd battleEnd)
        {
            _battleEnded = true;
        }
        
        private void OnDestroy()
        {
            if (_targetHealth != null)
            {
                _targetHealth.OnTotalHealthChangeEvent -= HandleTargetHealthChanged;
                _targetHealth.OnUndyingTriggeredEvent -= HandleTargetUndyingTriggered;
                _targetHealth.RemoveUndyingModifier(this);
            }
            
            if (_turnManager != null && _turnManager.OnBattleEndedCondition == _eventBattleEndedCondition)
                _turnManager.OnBattleEndedCondition = _previousBattleEndedCondition;
            
            if (ReferenceEquals(_activeController, this))
                _activeController = null;
        }
        
    }
}
