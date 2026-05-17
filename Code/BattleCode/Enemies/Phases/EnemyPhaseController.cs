using System.Collections.Generic;
using System.Linq;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.Phases;
using PSB.Code.BattleCode.Entities;
using PSW.Code.EventBus;
using PSB.Code.BattleCode.Enemies.BTs.Events;
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

        public void Initialize(ModuleOwner owner)
        {
            _battleEnemy = owner as BattleEnemy;
            _entityHealth = owner.GetModule<EntityHealth>();
            _enemyAttack = owner.GetModule<EnemyAttack>();
        }

        private void Start()
        {
            if (_battleEnemy != null && _battleEnemy.enemySO != null)
            {
                if (_battleEnemy.enemySO.phases != null && _battleEnemy.enemySO.phases.Length > 0)
                {
                    _sortedPhases = _battleEnemy.enemySO.phases
                        .OrderByDescending(p => p.hpThresholdPercent)
                        .ToList();
                }
                else
                {
                    Debug.Log($"[EnemyPhaseController] " +
                              $"{_battleEnemy.gameObject.name}에 설정된 페이즈 데이터가 없습니다");
                }
            }
            else
            {
                Debug.LogError("[EnemyPhaseController] Start 시점에서 EnemySO를 찾을 수 없습니다");
            }

            if (_entityHealth != null)
            {
                _entityHealth.OnHealthChangeEvent += HandleHealthChanged;
            }
        }

        private void OnDestroy()
        {
            if (_entityHealth != null)
            {
                _entityHealth.OnHealthChangeEvent -= HandleHealthChanged;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (max <= 0 || _battleEnemy.IsDead) return;

            float currentPercent = current / max;

            while (_currentPhaseIndex < _sortedPhases.Count)
            {
                if (currentPercent <= _sortedPhases[_currentPhaseIndex].hpThresholdPercent)
                {
                    ExecutePhaseAction(_sortedPhases[_currentPhaseIndex]);
                    _currentPhaseIndex++;
                }
                else
                {
                    break; 
                }
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
                        Bus<EnemySkillsChangedEvent>.Raise(new EnemySkillsChangedEvent(_battleEnemy, phaseData.phaseSkills));
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
                    else
                    {
                        Debug.LogWarning("[EnemyPhaseController] 소환할 적 데이터가 비어있습니다.");
                    }
                    break;
            }
        }
        
    }
}