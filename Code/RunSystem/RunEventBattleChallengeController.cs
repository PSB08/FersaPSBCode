using System;
using System.Collections;
using CIW.Code.System.Events;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Entities;
using PSW.Code.EventBus;
using UnityEngine;
using Work.CSH.Scripts.Managers;

namespace Work.PSB.Code.RunSystem
{
    public sealed class RunEventBattleChallengeController : MonoBehaviour
    {
        private RunEventSO _eventData;
        private EntityHealth _targetHealth;
        private TurnManagerSO _turnManager;
        private Func<bool> _previousBattleEndedCondition;
        private Func<bool> _challengeBattleEndedCondition;
        private Coroutine _battleEndCoroutine;
        
        private float _previousTargetHealth;
        private float _accumulatedDamage;
        private int _startedPlayerTurns;
        private bool _battleEnded;
        
        public void Initialize(RunEventSO eventData)
        {
            _eventData = eventData;
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
            BattleEnemy[] enemies = FindObjectsByType<BattleEnemy>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            
            for (int i = 0; i < enemies.Length; i++)
            {
                BattleEnemy enemy = enemies[i];
                if (enemy == null || enemy.EncounterIndex != _eventData.battleChallengeTargetEnemyIndex)
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
            _previousTargetHealth = health.TotalCurrentHealth;
            _startedPlayerTurns = 0;
            
            if (_eventData.battleChallengeProtectTarget)
                _targetHealth.AddUndyingModifier(this);
            
            _targetHealth.OnTotalHealthChangeEvent += HandleTargetHealthChanged;
            _turnManager.OnTurnStarted += HandleTurnStarted;
            
            _previousBattleEndedCondition = _turnManager.OnBattleEndedCondition;
            _challengeBattleEndedCondition = CheckBattleEnded;
            _turnManager.OnBattleEndedCondition = _challengeBattleEndedCondition;
        }
        
        private void HandleTargetHealthChanged(float currentHealth, float maxHealth)
        {
            if (_battleEnded)
                return;
            
            if (_turnManager != null && _turnManager.Turn && currentHealth < _previousTargetHealth)
                _accumulatedDamage += _previousTargetHealth - currentHealth;
            
            _previousTargetHealth = currentHealth;
        }
        
        private void HandleTurnStarted(bool isPlayerTurn)
        {
            if (isPlayerTurn && !_battleEnded)
                _startedPlayerTurns++;
        }
        
        private bool CheckBattleEnded()
        {
            if (_battleEnded || _battleEndCoroutine != null)
                return true;
            
            if (_turnManager != null && _turnManager.Turn)
            {
                if (_accumulatedDamage >= Mathf.Max(1f, _eventData.battleChallengeRequiredDamage))
                {
                    BeginChallengeEnd(RunEventBattleResult.Success);
                    return true;
                }
                
                if (_startedPlayerTurns >= Mathf.Max(1, _eventData.battleChallengeTurnLimit))
                {
                    BeginChallengeEnd(RunEventBattleResult.Failure);
                    return true;
                }
            }
            
            if (_previousBattleEndedCondition != null && _previousBattleEndedCondition.Invoke())
            {
                RunEventBattleResult result = _accumulatedDamage >=
                    Mathf.Max(1f, _eventData.battleChallengeRequiredDamage)
                    ? RunEventBattleResult.Success : RunEventBattleResult.Failure;
                BeginChallengeEnd(result);
                return true;
            }
            
            return false;
        }
        
        private void BeginChallengeEnd(RunEventBattleResult result)
        {
            if (_battleEndCoroutine != null || _battleEnded)
                return;
            
            RunStateStore.SetEventBattleResult(result);
            _battleEndCoroutine = StartCoroutine(CompleteBattleAfterDelay());
        }
        
        private IEnumerator CompleteBattleAfterDelay()
        {
            float delay = _eventData != null ? Mathf.Max(0f, _eventData.battleChallengeEndDelay) : 0f;
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            
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
                _targetHealth.RemoveUndyingModifier(this);
            }
            
            if (_turnManager != null)
            {
                _turnManager.OnTurnStarted -= HandleTurnStarted;
                
                if (_turnManager.OnBattleEndedCondition == _challengeBattleEndedCondition)
                    _turnManager.OnBattleEndedCondition = _previousBattleEndedCondition;
            }
        }
        
    }
}
