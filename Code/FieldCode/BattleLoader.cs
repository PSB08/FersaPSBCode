using System;
using System.Collections;
using CIW.Code;
using CIW.Code.System.Events;
using Code.Scripts.Entities;
using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSB.Code.BattleCode.BattleSystems;
using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.Players;
using PSW.Code.EventBus;
using UnityEngine;
using Work.PSB.Code.CoreSystem.Tests;
using Work.PSB.Code.CoreSystem.UpgradeSystem;

namespace Work.PSB.Code.FieldCode
{
    public class BattleLoader : MonoBehaviour
    {
        [SerializeField] private EnemyFactory enemyFactory;
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private MilestoneRewardApplier rewardApplier;

        [Inject] private PoolManagerMono _poolManager;
        [Inject] private PlayerManager _playerManager;

        private void Awake()
        {
            KillCounter.Instance?.TakeSnapshot();
            BattleLootSession.Instance?.Clear();

            Bus<PhaseClearEvent>.OnEvent += HandlePhaseClear;
            Bus<SpawnAdditionalEnemiesEvent>.OnEvent += HandleSpawnAdditionalEnemies;

            ApplyPresentation();
        }

        private void OnDestroy()
        {
            Bus<PhaseClearEvent>.OnEvent -= HandlePhaseClear;
            Bus<SpawnAdditionalEnemiesEvent>.OnEvent -= HandleSpawnAdditionalEnemies;
        }

        private IEnumerator Start()
        {
            yield return null;
            ApplyMilestone();
            
            StartCurrentPhase();
        }

        private void StartCurrentPhase()
        {
            var enemies = BattleRuntimeData.GetCurrentPhaseEnemies();
            
            if (enemies == null || enemies.Length == 0)
            {
                Debug.LogWarning("[BattleLoader] BattleRuntimeData.Enemies is empty.");
                Bus<BattleEnd>.Raise(new BattleEnd(true));
                return;
            }

            foreach (var enemyData in enemies)
            {
                if (enemyData == null)
                {
                    Debug.LogWarning("[BattleLoader] Encountered null EnemySO in runtime data.");
                    continue;
                }

                enemyFactory.CreateEnemy(enemyData, _poolManager);
            }

            int totalPhases = BattleRuntimeData.EncounterData != null ? 
                BattleRuntimeData.EncounterData.phases.Length : 1;
            
            Bus<PhaseStartEvent>.Raise(new PhaseStartEvent(BattleRuntimeData.CurrentPhaseIndex, 
                totalPhases, enemies));
        }

        private void HandlePhaseClear(PhaseClearEvent evt)
        {
            if (BattleRuntimeData.MoveToNextPhase())
            {
                Debug.Log($"[BattleLoader] 페이즈 {BattleRuntimeData.CurrentPhaseIndex + 1} 시작!");
                StartCurrentPhase();
            }
            else
            {
                Debug.Log("[BattleLoader] 모든 페이즈 클리어. 전투 승리!");
                Bus<BattleEnd>.Raise(new BattleEnd(true));
            }
        }

        private void HandleSpawnAdditionalEnemies(SpawnAdditionalEnemiesEvent evt)
        {
            if (evt.EnemiesToSpawn == null || evt.EnemiesToSpawn.Length == 0) return;

            foreach (var enemyData in evt.EnemiesToSpawn)
            {
                if (enemyData != null)
                {
                    enemyFactory.CreateEnemy(enemyData, _poolManager);
                }
            }
        }

        private void ApplyPresentation()
        {
            if (background == null)
            {
                Debug.LogWarning("[BattleLoader] Background SpriteRenderer is not assigned.");
                return;
            }

            if (BattleRuntimeData.Presentation == null)
            {
                Debug.LogWarning("[BattleLoader] Presentation is null.");
                return;
            }

            background.sprite = BattleRuntimeData.Presentation.backSprite;
        }

        private void ApplyMilestone()
        {
            Entity playerEntity = _playerManager.BattlePlayer; 
    
            UpgradeService upgradeService = new UpgradeService(playerEntity.GetModule<EntityStat>());

            if (rewardApplier != null)
            {
                rewardApplier.ApplyAllMilestones(playerEntity, upgradeService);
            }
        }
        
    }
}