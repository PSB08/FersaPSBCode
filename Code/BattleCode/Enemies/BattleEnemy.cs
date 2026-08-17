using CIW.Code;
using Code.Scripts.Entities;
using PSB_Lib.Dependencies;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.BTs;
using PSB.Code.BattleCode.Enemies.BTs.Events;
using PSB.Code.BattleCode.Enemies.Mechanics;
using PSB.Code.BattleCode.Enemies.PhaseBreak;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Skills;
using PSW.Code.EventBus;
using Unity.Behavior;
using UnityEngine;
using Work.CSH.Scripts.Interfaces;
using Work.CSH.Scripts.Managers;
using YIS.Code.Defines;
using YIS.Code.Modules;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies
{
    [RequireComponent(typeof(EnemyMechanicController))]
    public abstract class BattleEnemy : Entity, IModule, ITurnable, IBattleEnemyStateSender
    {
        [field: SerializeField] public EnemySO enemySO;
        [field: SerializeField] public EntityRenderer animator;
        
        [Header("Anim")]
        [SerializeField] protected SpriteRenderer outlineRenderer;
        
        [field: SerializeField] public TurnManagerSO TurnManager { get; set; }
        public BehaviorGraphAgent BtAgent { get; private set; }
        public int EncounterIndex { get; private set; } = -1;
        
        private ItemDropper _itemDropper;
        private EnemyHitReaction _hitReaction;
        private ChangeNewState _stateChannel;
        
        private EntityHealth _health;
        private EnemyAttack _enemyAttack;
        private EnemyPhaseBreakController _phaseBreakController;
        private bool _deadAnimStarted;
        private bool _deathStartedNotified;
        private bool _deathAnimationEndedNotified;
        
        private EnemyMechanicController _mechanicController;
        
        protected string StateChannelKey => "ChangeNewState";
        public BuffModule buffModule;
        public EnemyMechanicController MechanicController => _mechanicController;
        
        public virtual void Initialize(ModuleOwner owner)
        {
            animator = owner.GetModule<EntityRenderer>();
			
            if (animator != null)
            {
                animator.OnDeadEndTrigger -= HandleDeathAnimationEnded;
                animator.OnDeadEndTrigger += HandleDeathAnimationEnded;
            }
			
            _itemDropper = owner.GetModule<ItemDropper>();
            BtAgent = GetComponent<BehaviorGraphAgent>();
            
            _hitReaction = GetComponent<EnemyHitReaction>();
            buffModule = owner.GetModule<BuffModule>();
            _health = owner.GetModule<EntityHealth>();
            
            if (_health != null)
            {
                _health.RaiseDeathElementalEventOnDeathStart = true;
                _health.OnDeathSequenceStarted -= HandleDeathSequenceStarted;
                _health.OnDeathSequenceStarted += HandleDeathSequenceStarted;
            }
            
            _enemyAttack = owner.GetModule<EnemyAttack>();
            
            _phaseBreakController = GetComponent<EnemyPhaseBreakController>();
            if (_phaseBreakController != null) 
                _phaseBreakController.Initialize(owner);
            
            _mechanicController = owner.GetModule<EnemyMechanicController>();
            if (_mechanicController != null)
                _mechanicController.Initialize(owner);
            
            /*if (_hitReaction != null)
                _hitReaction.InitializeAnim();*/
        }
        
        protected override void Awake()
        {
            base.Awake();
            
            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);
            
            if (TurnManager != null)
                TurnManager.AddITurnableList(this);
            
            OnAwakeInternal();
        }
        
        //awake 때 따로 할 거 있으면
        protected virtual void OnAwakeInternal() { }
        
        private BlackboardVariable<T> GetBlackboardVariable<T>(string key)
        {
            if (BtAgent != null && BtAgent.GetVariable(key, out BlackboardVariable<T> result))
                return result;
            
            return default;
        }
        
        public void Setup(EnemySO data)
        {
            enemySO = data;
            
            Animator visualAnimator = animator.Animator;
            SpriteRenderer visualRenderer = animator.SpriteRenderer;
            
            if (visualAnimator != null && enemySO != null && enemySO.animController != null)
                visualAnimator.runtimeAnimatorController = enemySO.animController;
            
            // Outline Sync
            if (outlineRenderer != null && visualRenderer != null)
            {
                outlineRenderer.sprite = visualRenderer.sprite;
                outlineRenderer.flipX  = visualRenderer.flipX;
                outlineRenderer.flipY  = visualRenderer.flipY;
            }
            
            // Stat Override
            if (enemySO != null && enemySO.statOverrides != null && enemySO.statOverrides.Length > 0)
            {
                EntityStat statComp = GetModule<EntityStat>();
                if (statComp != null)
                {
                    statComp.OverrideStats(enemySO.statOverrides);
                    OnAfterOverrideStats(statComp);
                }
            }
            
            if (_itemDropper != null && enemySO != null)
                _itemDropper.SetDropTable(enemySO.dropTable);
            
            _mechanicController?.Setup(enemySO != null ? enemySO.mechanicSet : null);
            
            // Attack Skills
            EnemyAttack attackComp = GetModule<EnemyAttack>();
            if (attackComp != null && enemySO != null)
                attackComp.BeginBattleSkills(enemySO.attackSkills);
        }
        
        public void SetEncounterIndex(int encounterIndex)
        {
            EncounterIndex = encounterIndex;
        }
        
        protected virtual void OnAfterOverrideStats(EntityStat statComp) { }
        
        protected override void Start()
        {
            base.Start();
            
            TryResolveStateChannel();
            
            if (enemySO != null)
                gameObject.name = enemySO.name;
            
            OnStartInternal();
            _mechanicController?.NotifyBattleStarted();
        }
        
        //시작 때 따로 할 거 있으면
        protected virtual void OnStartInternal() { }
        
        protected override void Die()
        {
            IsDead = true;
            
            _mechanicController?.NotifyDied();
            
            if (TurnManager != null)
                TurnManager.RemoveITurnableList(this);
            
            if (_itemDropper != null)
                _itemDropper.DropItem();
            
            OnDieInternal();
        }
        
        //죽을 때 따로 할 거 있으면
        protected virtual void OnDieInternal()
        {
            if (enemySO != null)
            {
                EnemyIdentity identity = new EnemyIdentity
                {
                    EnemyName = enemySO.enemyName,
                    Grade = enemySO.grade
                };
                
                Bus<EnemyKilledEvent>.Raise(new EnemyKilledEvent(identity));
            }
        }
        
        protected virtual void OnDestroy()
        {
            if (animator != null)
                animator.OnDeadEndTrigger -= HandleDeathAnimationEnded;
			
            if (_health != null)
                _health.OnDeathSequenceStarted -= HandleDeathSequenceStarted;
            
            if (TurnManager != null)
                TurnManager.RemoveITurnableList(this);
            
            OnDestroyInternal();
        }
        
        protected virtual void OnDestroyInternal() { }
        
        private void HandleDeathSequenceStarted(Elemental elemental)
        {
            DeadAnimRoute();
			
            if (_deathStartedNotified) return;
			
            _deathStartedNotified = true;
            _mechanicController?.NotifyDeathStarted();
        }
		
        private void HandleDeathAnimationEnded()
        {
            if (!_deadAnimStarted || _deathAnimationEndedNotified) return;
			
            _deathAnimationEndedNotified = true;
            _mechanicController?.NotifyDeathAnimationEnded();
        }
        
        public void HitAnimRoute()
        {
            if (IsDead) return;
            
            _enemyAttack?.NotifyHit();
            
            _stateChannel?.SendEventMessage(BattleEnemyState.Hit);
            if (_hitReaction != null)
                _hitReaction.PlayHit();
        }
        
        public void DeadAnimRoute()
        {
            if (_deadAnimStarted) return;
            
            _deadAnimStarted = true;
            IsDead = true;
            _stateChannel?.SendEventMessage(BattleEnemyState.Dead);
        }
        
        public abstract void OnStartTurn(bool isPlayerTurn);
        public abstract void OnEndTurn(bool isPlayerTurn);
        
        protected void NotifyMechanicStartTurn(bool isPlayerTurn)
        {
            _mechanicController?.NotifyTurnStarted(isPlayerTurn);
        }
        
        protected void NotifyMechanicEndTurn(bool isPlayerTurn)
        {
            _mechanicController?.NotifyTurnEnded(isPlayerTurn);
        }
        
        public void NotifyMechanicHitObserved()
        {
            _mechanicController?.NotifyHitObserved();
        }
        
        public void NotifyMechanicSkillsChanged(SkillDataSO[] skills, bool isBattleStart)
        {
            _mechanicController?.NotifySkillsChanged(skills, isBattleStart);
        }
        
        public void NotifyMechanicSkillStarted(SkillDataSO skillData)
        {
            _mechanicController?.NotifySkillStarted(skillData);
        }
        
        public void NotifyMechanicSkillFinished(SkillDataSO skillData, BtSkillUseResult result)
        {
            _mechanicController?.NotifySkillFinished(skillData, result);
        }
        
        public void SendBTState(BattleEnemyState state)
        {
            TrySendBTState(state);
        }
        
        public bool TrySendBTState(BattleEnemyState state)
        {
            if (!TryResolveStateChannel())
            {
                Debug.LogWarning($"[BattleEnemy] {name} could not send BT state {state}: state channel is missing.", this);
                return false;
            }
            
            _stateChannel.SendEventMessage(state);
            return true;
        }
        
        public bool CanSendBTState()
        {
            return TryResolveStateChannel();
        }
        
        private bool TryResolveStateChannel()
        {
            if (_stateChannel != null)
            {
                return true;
            }
            
            //BehaviorGraphAgent 블랙보드에서 ChangeNewState 채널 변수
            var bbVar = GetBlackboardVariable<ChangeNewState>(StateChannelKey);
            //찾은 채널 값을 캐싱해서 다음 전송 때 재사용
            _stateChannel = bbVar?.Value;
            //채널을 실제로 찾았는지 결과로 반환
            return _stateChannel != null;
        }
        
    
    }
}
