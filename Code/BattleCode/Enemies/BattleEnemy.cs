using System.Linq;
using CIW.Code;
using Code.Scripts.Entities;
using PSB_Lib.Dependencies;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.BTs;
using PSB.Code.BattleCode.Enemies.BTs.Events;
using PSB.Code.BattleCode.Entities;
using PSW.Code.EventBus;
using Unity.Behavior;
using UnityEngine;
using Work.CSH.Scripts.Interfaces;
using Work.CSH.Scripts.Managers;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Enemies
{
    public abstract class BattleEnemy : Entity, IModule, ITurnable, IBattleEnemyStateSender
    {
        [field: SerializeField] public EnemySO enemySO;
        [field: SerializeField] public EntityRenderer animator;

        [Header("Anim")]
        [SerializeField] protected SpriteRenderer outlineRenderer;

        [field: SerializeField] public TurnManagerSO TurnManager { get; set; }
        public BehaviorGraphAgent BtAgent { get; private set; }
        
        private ItemDropper _itemDropper;
        private EnemyHitReaction _hitReaction;
        private ChangeNewState _stateChannel;

        private EntityHealth _health;
        
        protected string StateChannelKey => "ChangeNewState";
        public BuffModule buffModule;

        public virtual void Initialize(ModuleOwner owner)
        {
            animator     = owner.GetModule<EntityRenderer>();
            _itemDropper = owner.GetModule<ItemDropper>();
            BtAgent      = GetComponent<BehaviorGraphAgent>();

            _hitReaction = GetComponent<EnemyHitReaction>();
            buffModule = owner.GetModule<BuffModule>();
            _health = owner.GetModule<EntityHealth>();
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

            // Attack Skills
            EnemyAttack attackComp = GetModule<EnemyAttack>();
            if (attackComp != null && enemySO != null)
                attackComp.SetAttackSkills(enemySO.attackSkills);
            
            InitPhaseSystem();
        }
        
        protected virtual void OnAfterOverrideStats(EntityStat statComp) { }

        protected override void Start()
        {
            base.Start();
            
            var bbVar = GetBlackboardVariable<ChangeNewState>(StateChannelKey);
            _stateChannel = bbVar?.Value;

            if (enemySO != null)
                gameObject.name = enemySO.name;

            OnStartInternal();
        }

        //시작 때 따로 할 거 있으면
        protected virtual void OnStartInternal() { }

        protected override void Die()
        {
            IsDead = true;
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
            if (TurnManager != null)
                TurnManager.RemoveITurnableList(this);

            OnDestroyInternal();
        }

        protected virtual void OnDestroyInternal() { }

        public void HitAnimRoute()
        {
            if (IsDead) return;
            
            _stateChannel?.SendEventMessage(BattleEnemyState.Hit);
            if (_hitReaction != null)
                _hitReaction.PlayHit();
        }

        public void DeadAnimRoute()
        {
            IsDead = true;
            _stateChannel?.SendEventMessage(BattleEnemyState.Dead);
        }

        public abstract void OnStartTurn(bool isPlayerTurn);
        public abstract void OnEndTurn(bool isPlayerTurn);

        public void SendBTState(BattleEnemyState state)
            => _stateChannel?.SendEventMessage(state);
        
        private void InitPhaseSystem()
        {
            if (enemySO == null || enemySO.phases == null || enemySO.phases.Length == 0) return;

            var thresholds = enemySO.phases
                .OrderByDescending(p => p.hpThresholdPercent)
                .Select(p => p.hpThresholdPercent)
                .ToList();

            if (_health != null)
            {
                _health.SetPhaseThresholds(thresholds);
            }
        }
        
    }
}