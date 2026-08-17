using System;
using System.Collections;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.Mechanics.ActionGates;
using PSB.Code.BattleCode.Enemies.Mechanics.Intents;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Skills;
using UnityEngine;
using YIS.Code.Modules;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Mechanics
{
    [DisallowMultipleComponent]
    public sealed class EnemyMechanicController : MonoBehaviour, IModule
    {
        private BattleEnemy _enemy;
        private EnemyMechanicHost _host;
        private Coroutine _battleReadyCoroutine;
        
        public bool IsInitialized => _host != null;
        
        public void Initialize(ModuleOwner owner)
        {
            DisposeHost();
            
            _enemy = owner as BattleEnemy;
            if (_enemy == null)
            {
                Debug.LogError("[EnemyMechanicController] BattleEnemy owner is required.", this);
                return;
            }
            
            EnemyAttack attack = owner.GetModule<EnemyAttack>();
            EntityHealth health = owner.GetModule<EntityHealth>();
            _host = new EnemyMechanicHost(_enemy, attack, health);
        }
        
        public void Setup(EnemyMechanicSetSO mechanicSet)
        {
            _host?.Setup(mechanicSet);
        }
        
        public bool TryResolveIntent(Func<EnemyIntentProposal, EnemyIntentAcceptance> tryAccept)
        {
            if (_host == null || _host.Context == null || tryAccept == null)
                return false;
            
            EnemyIntentRequest request = new EnemyIntentRequest(_host.Context);
            return _host.Intents.TryResolve(request, tryAccept);
        }
        
        public IEnumerator ResolveBeforeAction()
        {
            if (_host == null || _host.Context == null)
                yield break;
            
            EnemyActionGateContext context = new EnemyActionGateContext(_enemy, _host.Context);
            yield return _host.ActionGates.ResolveBeforeAction(context);
        }
        
        public void NotifyBattleStarted()
        {
            _host?.NotifyBattleStarted();

            if (_battleReadyCoroutine != null)
                StopCoroutine(_battleReadyCoroutine);

            _battleReadyCoroutine = StartCoroutine(NotifyBattleReadyRoutine());
        }
        
        public void NotifyTurnStarted(bool isPlayerTurn)
        {
            _host?.NotifyTurnStarted(isPlayerTurn);
        }
        
        public void NotifyTurnEnded(bool isPlayerTurn)
        {
            _host?.NotifyTurnEnded(isPlayerTurn);
        }
        
        public void NotifyHitObserved()
        {
            _host?.NotifyHitObserved();
        }
        
        public void NotifySkillsChanged(SkillDataSO[] skills, bool isBattleStart)
        {
            _host?.NotifySkillsChanged(skills, isBattleStart);
        }
        
        public void NotifySkillStarted(SkillDataSO skill)
        {
            _host?.NotifySkillStarted(skill);
        }
        
        public void NotifySkillFinished(SkillDataSO skill, BtSkillUseResult result)
        {
            _host?.NotifySkillFinished(skill, result);
        }
		
        public void NotifyDeathStarted()
        {
            _host?.NotifyDeathStarted();
        }
		
        public void NotifyDeathAnimationEnded()
        {
            _host?.NotifyDeathAnimationEnded();
        }
        
        public void NotifyDied()
        {
            _host?.NotifyDied();
        }
        
        private void OnDestroy()
        {
            DisposeHost();
        }
        
        private void DisposeHost()
        {
            if (_battleReadyCoroutine != null)
            {
                StopCoroutine(_battleReadyCoroutine);
                _battleReadyCoroutine = null;
            }
            
            _host?.Dispose();
            _host = null;
            _enemy = null;
        }
        
        private IEnumerator NotifyBattleReadyRoutine()
        {
            yield return null;
            
            _battleReadyCoroutine = null;
            _host?.NotifyBattleReady();
        }
        
    }
}
