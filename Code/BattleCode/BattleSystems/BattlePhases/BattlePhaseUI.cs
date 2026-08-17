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

        private int _totalEnemies = 0;

        private void Awake()
        {
            if (phaseText != null)
                phaseText.gameObject.SetActive(false);

            Bus<BattleEncounterStartEvent>.OnEvent += OnBattleStart;
            Bus<EnemyListChanged>.OnEvent += OnEnemyListChanged;
        }

        private void OnDestroy()
        {
            Bus<BattleEncounterStartEvent>.OnEvent -= OnBattleStart;
            Bus<EnemyListChanged>.OnEvent -= OnEnemyListChanged;
        }

        private void OnBattleStart(BattleEncounterStartEvent evt)
        {
            _totalEnemies = evt.Enemies != null ? evt.Enemies.Length : 0;
            UpdateUI();
        }

        private void OnEnemyListChanged(EnemyListChanged evt)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
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

                int killedCount = _totalEnemies - aliveCount;
                if (killedCount < 0) killedCount = 0;

                enemyCountText.text = $"Enemies : {killedCount} / {_totalEnemies}";
            }
        }
        
    }
}
