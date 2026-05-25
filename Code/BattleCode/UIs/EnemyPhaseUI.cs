using System.Collections.Generic;
using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace PSB.Code.BattleCode.UIs
{
    public class EnemyPhaseUI : MonoBehaviour
    {
        [SerializeField] private BattleEnemy ownerEnemy;
        
        [Header("Settings")]
        [SerializeField] private Transform iconRoot;
        [SerializeField] private PoolItemSO iconPrefab;

        [Inject] private PoolManagerMono _poolManager;

        private List<PhaseIcon> _activeIcons = new List<PhaseIcon>();
        
        private bool _isInitialized = false;
        private int _totalPhasesMemory = 0;

        private void Awake()
        {
            if (Injector.Instance != null)
            {
                Injector.Instance.InjectTo(this);
            }
        }

        private void OnEnable()
        {
            Bus<EnemyPhaseChangedEvent>.OnEvent += OnPhaseChanged;
        }

        private void OnDisable()
        {
            Bus<EnemyPhaseChangedEvent>.OnEvent -= OnPhaseChanged;
        }

        private void OnPhaseChanged(EnemyPhaseChangedEvent evt)
        {
            if (iconPrefab == null || _poolManager == null) return;
            
            if (evt.TargetEnemy != ownerEnemy) return;

            if (evt.TotalPhases <= 0) return;
            
            if (!_isInitialized)
            {
                InitializeIcons(evt.TotalPhases);
                _isInitialized = true;
            }

            UpdateIconsBasedOnPhase(evt.CurrentPhaseNum);
        }

        private void InitializeIcons(int totalPhases)
        {
            ClearAllIcons();
            _totalPhasesMemory = totalPhases;

            for (int i = 0; i < totalPhases; i++)
            {
                PhaseIcon icon = _poolManager.Pop<PhaseIcon>(iconPrefab);
                
                if (icon != null)
                {
                    icon.transform.SetParent(iconRoot);
                    icon.transform.localScale = Vector3.one;
                    icon.Init();
                    
                    _activeIcons.Add(icon);
                }
            }
        }

        private void UpdateIconsBasedOnPhase(int currentPhaseNum)
        {
            int activeCount = _totalPhasesMemory - currentPhaseNum + 1;
            
            int consumedCount = _activeIcons.Count - activeCount;

            for (int i = 0; i < _activeIcons.Count; i++)
            {
                if (i < consumedCount)
                {
                    _activeIcons[i].SetState(false);
                }
                else
                {
                    _activeIcons[i].SetState(true);
                }
            }
        }

        private void ClearAllIcons()
        {
            foreach (var icon in _activeIcons)
            {
                if (icon != null)
                {
                    _poolManager.Push(icon);
                }
            }
            _activeIcons.Clear();
            _isInitialized = false;
        }
        
    }
}