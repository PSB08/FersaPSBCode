using System;
using System.Collections;
using CIW.Code;
using CIW.Code.System.Events;
using Code.Scripts.Entities;
using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSB.Code.BattleCode.BattleSystems;
using PSB.Code.BattleCode.Enemies;
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
        [Inject] private BattleEnemyManager _enemyManager;

        private void Awake()
        {
            KillCounter.Instance?.TakeSnapshot();
            BattleLootSession.Instance?.Clear();

            ApplyPresentation();
        }

        private IEnumerator Start()
        {
            yield return null;
            ApplyMilestone();
            
            StartBattle();
        }

        private void StartBattle()
        {
            var enemies = BattleRuntimeData.GetCurrentEnemies();
            
            if (enemies == null || enemies.Length == 0)
            {
                Debug.LogWarning("[BattleLoader] BattleRuntimeData.Enemies is empty.");
                Bus<BattleEnd>.Raise(new BattleEnd(true));
                return;
            }

            BattleEnemyManager enemyManager = ResolveEnemyManager();
            enemyManager?.BeginEncounter();

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemySO enemyData = enemies[i];
                if (enemyData == null)
                {
                    Debug.LogWarning("[BattleLoader] Encountered null EnemySO in runtime data.");
                    continue;
                }

                BattleEnemy instance = enemyFactory.CreateEnemy(enemyData, _poolManager);
                if (instance != null)
                    instance.SetEncounterIndex(i);
            }

            enemyManager?.StartBattleEncounter();
            Bus<BattleEncounterStartEvent>.Raise(new BattleEncounterStartEvent(enemies));
        }

        private BattleEnemyManager ResolveEnemyManager()
        {
            if (_enemyManager == null)
                _enemyManager = FindAnyObjectByType<BattleEnemyManager>();

            return _enemyManager;
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
