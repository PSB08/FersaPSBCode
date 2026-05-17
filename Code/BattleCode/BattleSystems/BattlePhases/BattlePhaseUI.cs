using PSB.Code.CoreSystem.Events;
using PSB_Lib.Dependencies;
using PSW.Code.EventBus;
using TMPro;
using UnityEngine;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Events;

namespace Work.PSB.Code.FieldCode.UI
{
    public class BattlePhaseUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI phaseText; 
        [SerializeField] private TextMeshProUGUI enemyCountText; 

        [Inject] private BattleEnemyManager _enemyManager;

        private int _currentPhaseIndex = 0;
        private int _totalPhases = 1;
        
        private int _totalEnemiesAllPhases = 0;
        private int _totalSpawnedUpToCurrent = 0;

        private void Awake()
        {
            Bus<PhaseStartEvent>.OnEvent += OnPhaseStart;
            Bus<EnemyListChanged>.OnEvent += OnEnemyListChanged;
        }

        private void OnDestroy()
        {
            Bus<PhaseStartEvent>.OnEvent -= OnPhaseStart;
            Bus<EnemyListChanged>.OnEvent -= OnEnemyListChanged;
        }

        private void OnPhaseStart(PhaseStartEvent evt)
        {
            _currentPhaseIndex = evt.PhaseIndex + 1; 
            _totalPhases = evt.TotalPhases;

            _totalEnemiesAllPhases = 0;
            _totalSpawnedUpToCurrent = 0;

            if (BattleRuntimeData.EncounterData != null)
            {
                for (int i = 0; i < BattleRuntimeData.EncounterData.phases.Length; i++)
                {
                    int phaseEnemyCount = BattleRuntimeData.EncounterData.phases[i].enemies.Length;
                    _totalEnemiesAllPhases += phaseEnemyCount;
                    
                    if (i <= evt.PhaseIndex)
                    {
                        _totalSpawnedUpToCurrent += phaseEnemyCount;
                    }
                }
            }
            else
            {
                _totalEnemiesAllPhases = evt.PhaseEnemies.Length;
                _totalSpawnedUpToCurrent = evt.PhaseEnemies.Length;
            }

            UpdateUI();
        }

        private void OnEnemyListChanged(EnemyListChanged evt)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (phaseText != null)
            {
                phaseText.text = $"Phase {_currentPhaseIndex} / {_totalPhases}";
            }

            if (enemyCountText != null)
            {
                int aliveCount = 0;
                if (_enemyManager != null && _enemyManager.GetEnemies() != null)
                {
                    foreach (var enemy in _enemyManager.GetEnemies())
                    {
                        if (enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
                        {
                            aliveCount++;
                        }
                    }
                }

                int killedCount = _totalSpawnedUpToCurrent - aliveCount;
                if (killedCount < 0) killedCount = 0;

                enemyCountText.text = $"Enemies : {killedCount} / {_totalEnemiesAllPhases}";
            }
        }
        
    }
}