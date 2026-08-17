using Code.Scripts.Entities;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB_Lib.Dependencies;
using PSB.Code.BattleCode.Players;
using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;

namespace PSB.Code.BattleCode.Enemies
{
    public class NormalBattleEnemy : BattleEnemy
    {
        [Inject] private BattleEnemyManager _battleEnemyManager;
        [Inject] private PlayerManager _playerManager;
        
        public PlayerManager PlayerManager => _playerManager;
        
        protected override void OnAwakeInternal()
        {
            Bus<EnemyProgressionStageChanged>.OnEvent += OnStageChanged;
        }
        
        protected override void OnStartInternal()
        {
            ApplyProgression(EnemyProgressionManager.CurrentStage);
        }
        
        protected override void OnAfterOverrideStats(EntityStat statComp)
        {
            ApplyProgression(EnemyProgressionManager.CurrentStage);
        }
        
        private void OnStageChanged(EnemyProgressionStageChanged evt)
        {
            ApplyProgression(evt.stage);
        }
        
        private void ApplyProgression(int stage)
        {
            if (enemySO == null) return;
            if (enemySO.progressionSO == null) return;
            if (enemySO.statOverrides == null || enemySO.statOverrides.Length == 0) return;
            
            EntityStat statComp = GetModule<EntityStat>();
            if (statComp == null) return;
            
            var applier = new EnemyStatProgressionApplier(enemySO.progressionSO, stage);
            applier.Clear(statComp, enemySO.statOverrides);
            applier.Apply(statComp, enemySO.statOverrides);
        }
        
        public override void OnStartTurn(bool isPlayerTurn)
        {
            NotifyMechanicStartTurn(isPlayerTurn);
            
            if (!isPlayerTurn && _battleEnemyManager != null)
                _battleEnemyManager.TryStartEnemyAttackSequence();
        }
        
        public override void OnEndTurn(bool isPlayerTurn)
        {
            NotifyMechanicEndTurn(isPlayerTurn);
            
            if (!isPlayerTurn && _battleEnemyManager != null)
                _battleEnemyManager.ResetAttackSequenceFlag();
        }
        
        protected override void OnDestroyInternal()
        {
            if (_battleEnemyManager != null)
                _battleEnemyManager.Unregister(this);
            
            Bus<EnemyProgressionStageChanged>.OnEvent -= OnStageChanged;
        }
        
        protected override void OnDieInternal()
        {
            //기본 사망 처리로 처치 이벤트, 드랍 등 공통 로직을 먼저
            base.OnDieInternal();
            
            if (_battleEnemyManager != null)
            {
                //죽은 적 오브젝트가 실제로 빠진 뒤 대기열 적을 넣어 정렬, 공격 순서가 꼬이지 않게
                _battleEnemyManager.Unregister(this);
            }
        }
        
        public void RegisterToManager()
        {
            if (_battleEnemyManager != null)
                _battleEnemyManager.Register(this);
        }
    
    }
}
