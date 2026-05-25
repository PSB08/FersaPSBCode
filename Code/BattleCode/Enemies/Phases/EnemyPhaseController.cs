using System.Collections.Generic;
using System.Linq;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.Phases;
using PSB.Code.BattleCode.Entities;
using PSW.Code.EventBus;
using PSB.Code.BattleCode.Events;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Enemies
{
    public class EnemyPhaseController : MonoBehaviour, IModule
    {
        private BattleEnemy _battleEnemy;
        private EntityHealth _entityHealth;
        private EnemyAttack _enemyAttack;

        private List<EnemyPhaseData> _sortedPhases = new();
        private int _currentPhaseIndex = 0;

        private int _currentUIPhaseIndex = 0; 
        private int _totalUIPhases = 0;

        public int CurrentPhaseNum => _currentUIPhaseIndex + 1;
        public int TotalPhases => _totalUIPhases;

        public void Initialize(ModuleOwner owner)
        {
            _battleEnemy = owner as BattleEnemy;
            _entityHealth = owner.GetModule<EntityHealth>();
            _enemyAttack = owner.GetModule<EnemyAttack>();
        }

        private System.Collections.IEnumerator Start()
        {
            if (_battleEnemy != null && _battleEnemy.enemySO != null)
            {
                if (_battleEnemy.enemySO.phases != null && _battleEnemy.enemySO.phases.Length > 0)
                {
                    _sortedPhases = _battleEnemy.enemySO.phases
                        .OrderByDescending(p => p.hpThresholdPercent)
                        .ToList();

                    _totalUIPhases = _sortedPhases.Select(p => p.hpThresholdPercent).Distinct().Count();
                }
            }

            if (_entityHealth != null)
            {
                _entityHealth.OnTotalHealthChangeEvent += HandleHealthChanged;
            }

            yield return null;

            if (TotalPhases > 0)
            {
                Bus<EnemyPhaseChangedEvent>.Raise(new EnemyPhaseChangedEvent(_battleEnemy, CurrentPhaseNum, TotalPhases));
            }
        }

        private void OnDestroy()
        {
            if (_entityHealth != null)
            {
                _entityHealth.OnTotalHealthChangeEvent -= HandleHealthChanged;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (max <= 0 || _battleEnemy.IsDead) return;

            float currentPercent = current / max;
            bool phaseChangedThisHit = false;

            while (_currentPhaseIndex < _sortedPhases.Count)
            {
                float targetThreshold = _sortedPhases[_currentPhaseIndex].hpThresholdPercent;

                if (currentPercent <= targetThreshold)
                {
                    while (_currentPhaseIndex < _sortedPhases.Count && 
                           Mathf.Approximately(_sortedPhases[_currentPhaseIndex].hpThresholdPercent, targetThreshold))
                    {
                        var phaseData = _sortedPhases[_currentPhaseIndex];
                        _currentPhaseIndex++;
                        ExecutePhaseAction(phaseData);
                    }

                    _currentUIPhaseIndex++;
                    phaseChangedThisHit = true;
                }
                else
                {
                    break; 
                }
            }

            if (phaseChangedThisHit && TotalPhases > 0)
            {
                Bus<EnemyPhaseChangedEvent>.Raise(new EnemyPhaseChangedEvent(_battleEnemy, CurrentPhaseNum, TotalPhases));
            }
        }

        private void ExecutePhaseAction(EnemyPhaseData phaseData)
        {
            switch (phaseData.actionType)
            {
                case PhaseActionType.ChangeSkills:
                    if (_enemyAttack != null && phaseData.phaseSkills != null && phaseData.phaseSkills.Length > 0)
                    {
                        _enemyAttack.SetAttackSkills(phaseData.phaseSkills);
                    }
                    break;

                case PhaseActionType.ApplyBuffs:
                    if (phaseData.phaseBuffs != null)
                    {
                        foreach (var buffEffect in phaseData.phaseBuffs)
                        {
                            if (buffEffect != null)
                            {
                                buffEffect.ApplyEffect(_battleEnemy);
                            }
                        }
                    }
                    break;

                case PhaseActionType.SpawnEnemies:
                    if (phaseData.phaseEnemies != null && phaseData.phaseEnemies.Length > 0)
                    {
                        Bus<SpawnAdditionalEnemiesEvent>.Raise(new SpawnAdditionalEnemiesEvent(phaseData.phaseEnemies));
                    }
                    break;
            }
        }
        
    }
}