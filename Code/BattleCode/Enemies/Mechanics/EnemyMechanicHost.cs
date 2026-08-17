using System;
using System.Collections.Generic;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.Mechanics.ActionGates;
using PSB.Code.BattleCode.Enemies.Mechanics.Intents;
using PSB.Code.BattleCode.Enemies.Mechanics.Phases;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Players;
using PSB.Code.BattleCode.Skills;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Mechanics
{
    public sealed class EnemyMechanicHost : IDisposable
    {
        private readonly BattleEnemy _enemy;
        private readonly EnemyAttack _attack;
        private readonly EntityHealth _health;
        private readonly List<EnemyMechanicRuntime> _runtimes = new();
        private readonly EnemyBattleMechanicScope _fallbackScope = new();
        
        private EnemyMechanicSubscriptionBag _externalRegistrations =
            new EnemyMechanicSubscriptionBag();
        
        private BattleEnemyManager _enemyManager;
        private PlayerManager _playerManager;
        private EnemyBattleMechanicScope _battleScope;
        private float _previousHealth;
        private bool _battleStarted;
        private bool _battleReady;
        private bool _disposed;
        
        public EnemyMechanicSignalHub Signals { get; } = new EnemyMechanicSignalHub();
        public EnemyIntentResolver Intents { get; } = new EnemyIntentResolver();
        public EnemyPhaseTransitionPipeline PhaseTransitions { get; } =
            new EnemyPhaseTransitionPipeline();
        public EnemyActionGatePipeline ActionGates { get; } = new EnemyActionGatePipeline();
        
        public EnemyMechanicContext Context { get; private set; }
        
        public EnemyMechanicHost(BattleEnemy enemy, EnemyAttack attack, EntityHealth health)
        {
            _enemy = enemy;
            _attack = attack;
            _health = health;
            
            if (_health != null)
            {
                _previousHealth = _health.CurrentHealth;
                _health.OnTotalHealthChangeEvent += HandleHealthChanged;
            }
        }
        
        public void Setup(EnemyMechanicSetSO mechanicSet)
        {
            if (_disposed) return;
            
            ClearRuntimes();
            ClearExternalRegistrations();
            ResolveBattleScope();
            
            Context = new EnemyMechanicContext(_enemy, _attack, _health, _enemyManager,
                _playerManager, this, _battleScope, Signals, Intents, PhaseTransitions, ActionGates);
            
            RegisterExternalExtensions();
            
            EnemyMechanicSO[] mechanics = mechanicSet != null ? mechanicSet.Mechanics : null;
            
            if (mechanics != null)
            {
                for (int i = 0; i < mechanics.Length; i++)
                {
                    EnemyMechanicSO mechanic = mechanics[i];
                    if (mechanic == null) continue;
                    
                    try
                    {
                        EnemyMechanicRuntime runtime = mechanic.CreateRuntime(Context);
                        if (runtime == null) continue;
                        
                        _runtimes.Add(runtime);
                        runtime.Attach();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception, mechanic);
                    }
                }
            }
            
            PublishInitialHealth();
            
            if (_battleStarted)
                Publish(new EnemyBattleStartedSignal(_enemy));
            
            if (_battleReady)
                Publish(new EnemyBattleReadySignal(_enemy));
        }
        
        public void NotifyBattleStarted()
        {
            _battleStarted = true;
            Publish(new EnemyBattleStartedSignal(_enemy));
        }
        
        public void NotifyBattleReady()
        {
            if (_battleReady)
                return;
            
            _battleReady = true;
            Publish(new EnemyBattleReadySignal(_enemy));
        }
        
        public void NotifyTurnStarted(bool isPlayerTurn)
        {
            Publish(new EnemyTurnStartedSignal(_enemy, isPlayerTurn));
        }
        
        public void NotifyTurnEnded(bool isPlayerTurn)
        {
            Publish(new EnemyTurnEndedSignal(_enemy, isPlayerTurn));
        }
        
        public void NotifyHitObserved()
        {
            Publish(new EnemyHitObservedSignal(_enemy));
        }
        
        public void NotifySkillsChanged(SkillDataSO[] skills, bool isBattleStart)
        {
            Publish(new EnemySkillsChangedSignal(_enemy, skills, isBattleStart));
        }
        
        public void NotifySkillStarted(SkillDataSO skill)
        {
            Publish(new EnemySkillStartedSignal(_enemy, skill));
        }
        
        public void NotifySkillFinished(SkillDataSO skill, BtSkillUseResult result)
        {
            Publish(new EnemySkillFinishedSignal(_enemy, skill, result));
        }
		
        public void NotifyDeathStarted()
        {
            Publish(new EnemyDeathStartedSignal(_enemy));
        }
		
        public void NotifyDeathAnimationEnded()
        {
            Publish(new EnemyDeathAnimationEndedSignal(_enemy));
        }
        
        public void NotifyDied()
        {
            Publish(new EnemyDiedSignal(_enemy));
            ClearRuntimes();
        }
        
        public void Publish<TSignal>(TSignal signal)
        {
            if (_disposed) return;
            
            Signals.Publish(signal);
            _battleScope?.Signals.Publish(signal);
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            ClearRuntimes();
            ClearExternalRegistrations();
            
            if (_health != null)
                _health.OnTotalHealthChangeEvent -= HandleHealthChanged;
            
            Signals.Clear();
            _fallbackScope.Dispose();
            Context = null;
        }
        
        private void ResolveBattleScope()
        {
            _enemyManager = UnityEngine.Object.FindAnyObjectByType<BattleEnemyManager>();
            _playerManager = UnityEngine.Object.FindAnyObjectByType<PlayerManager>();
            _battleScope = _enemyManager != null ? _enemyManager.MechanicScope : _fallbackScope;
        }
        
        private void RegisterExternalExtensions()
        {
            if (_enemy == null) return;
            
            MonoBehaviour[] behaviours = _enemy.GetComponentsInChildren<MonoBehaviour>(true);
            
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null) continue;
                
                if (behaviour is IEnemyIntentContributor intentContributor)
                    _externalRegistrations.Add(Intents.Register(intentContributor));
                
                if (behaviour is IEnemyPhaseTransitionGuard phaseGuard)
                    _externalRegistrations.Add(PhaseTransitions.Register(phaseGuard));
                
                if (behaviour is IEnemyActionGate actionGate)
                    _externalRegistrations.Add(ActionGates.Register(actionGate));
            }
        }
        
        private void PublishInitialHealth()
        {
            if (_health == null) return;
            
            _previousHealth = _health.CurrentHealth;
            Publish(new EnemyHealthChangedSignal(_enemy, _previousHealth,
                _health.CurrentHealth, _health.MaxHealth));
        }
        
        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            float previousHealth = _previousHealth;
            _previousHealth = currentHealth;
            
            Publish(new EnemyHealthChangedSignal(_enemy, previousHealth, currentHealth, maxHealth));
            
            if (currentHealth < previousHealth)
            {
                float damage = previousHealth - currentHealth;
                Publish(new EnemyDamagedSignal(_enemy, previousHealth,
                    currentHealth, maxHealth, damage));
            }
        }
        
        private void ClearExternalRegistrations()
        {
            _externalRegistrations.Dispose();
            _externalRegistrations = new EnemyMechanicSubscriptionBag();
        }
        
        private void ClearRuntimes()
        {
            for (int i = _runtimes.Count - 1; i >= 0; i--)
            {
                try
                {
                    _runtimes[i]?.Detach();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, _enemy);
                }
            }
            
            _runtimes.Clear();
        }
        
    }
}
