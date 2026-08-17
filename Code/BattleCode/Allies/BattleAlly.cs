using System;
using System.Collections;
using CIW.Code;
using CIW.Code.System.Events;
using Code.Scripts.Entities;
using PSB.Code.BattleCode.Allies.AttackCode;
using PSB.Code.BattleCode.Allies.BTs.Events;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Entities;
using PSB_Lib.Dependencies;
using PSW.Code.EventBus;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.UI;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Allies
{
    public class BattleAlly : Entity, IModule
    {
        [field: SerializeField] public AllySO allySO;
        [field: SerializeField] public EntityRenderer animator;

        [SerializeField] private bool registerOnStart = true;

        [Inject] private BattleAllyManager _allyManager;
        private AllyPartyService _partyService;

        private EntityHealth _health;
        private AllyAttack _allyAttack;
        private EnemyHitReaction _hitReaction;
        private ChangeAllyState _stateChannel;
        
        private bool _deadAnimStarted;
        private bool _healthRestored;
        private bool _healthSubscribed;
        private int _allyIndex = -1;
        private int _preferredSlotIndex = -1;

        private const string StateChannelKey = "ChangeAllyState";

        public BehaviorGraphAgent BtAgent { get; private set; }
        public int AllyIndex => _allyIndex;
        public bool IsHealthRestored => _healthRestored;
        public BuffModule buffModule;

        public void Initialize(ModuleOwner owner)
        {
            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);

            animator = owner.GetModule<EntityRenderer>();
            BtAgent = GetComponent<BehaviorGraphAgent>();
            buffModule = owner.GetModule<BuffModule>();
            _health = owner.GetModule<EntityHealth>();
            
            if (_health != null)
            {
                _health.RaiseDeathElementalEventOnDeathStart = true;
                _health.OnDeathSequenceStarted -= HandleDeathSequenceStarted;
                _health.OnDeathSequenceStarted += HandleDeathSequenceStarted;
            }

            _allyAttack = owner.GetModule<AllyAttack>();
            _hitReaction = GetComponent<EnemyHitReaction>();
        }

        private void OnEnable()
        {
            Bus<BattleEnd>.OnEvent += HandleBattleEnd;
            if (_healthRestored)
                TrySubscribeHealth();
        }

        private void OnDisable()
        {
            Bus<BattleEnd>.OnEvent -= HandleBattleEnd;
            UnsubscribeHealth();
        }

        protected override void Awake()
        {
            base.Awake();

            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);

            if (allySO != null)
                Setup(allySO);
        }

        protected override void Start()
        {
            base.Start();

            if (allySO != null)
                gameObject.name = allySO.name;

            TrySendBTState(BattleAllyState.Idle);
            StartCoroutine(RestoreHealthAfterInit());

            if (registerOnStart)
                RegisterToManager();
        }

        public void SetRegisterOnStart(bool value)
        {
            registerOnStart = value;
        }

        public void Setup(AllySO data)
        {
            if (data == null)
                return;

            allySO = data;

            if (animator != null && animator.Animator != null && allySO != null && allySO.animController != null)
                animator.Animator.runtimeAnimatorController = allySO.animController;

            if (allySO != null && allySO.statOverrides != null && allySO.statOverrides.Length > 0)
            {
                EntityStat statComp = GetModule<EntityStat>();
                if (statComp != null)
                    statComp.OverrideStats(allySO.statOverrides);
            }

            AllyAttack attackComp = GetModule<AllyAttack>();
            if (attackComp != null && allySO != null)
                attackComp.SetAttackSkills(allySO.attackSkills);
        }

        public bool RegisterToManager()
        {
            if (_allyManager == null)
                return false;

            if (IsDead)
                return false;

            bool registered = _preferredSlotIndex >= 0
                ? _allyManager.Register(this, _preferredSlotIndex) : _allyManager.Register(this);

            if (!registered)
                gameObject.SetActive(false);

            return registered;
        }

        public void SetPreferredSlotIndex(int index)
        {
            _preferredSlotIndex = index;
        }

        public void SetRenderersVisible(bool visible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = visible;

            CanvasGroup[] canvasGroups = GetComponentsInChildren<CanvasGroup>(true);
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                canvasGroups[i].alpha = visible ? 1f : 0f;
                canvasGroups[i].interactable = visible;
                canvasGroups[i].blocksRaycasts = visible;
            }

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].enabled = visible;
        }

        public void ApplyRewardHealth(AllyRewardResult result)
        {
            if (!result.HasHealthData)
                return;

            if (_health == null || !_health.IsInitialized || _health.MaxHealth <= 0f)
            {
                StartCoroutine(ApplyRewardHealthAfterInit(result));
                return;
            }

            ApplyCurrentHealth(result.CurrentHp);
        }

        public void SetAllyIndex(int index)
        {
            _allyIndex = index;
        }

        public void HitAnimRoute()
        {
            if (IsDead) return;

            _allyAttack?.NotifyHit();
            _hitReaction?.PlayHit();

            TrySendBTState(BattleAllyState.Hit);
        }

        public void DeadAnimRoute()
        {
            if (_deadAnimStarted) return;

            _deadAnimStarted = true;
            IsDead = true;

            if (_allyManager != null)
                _allyManager.Unregister(this);

            TrySendBTState(BattleAllyState.Dead);
        }

        public void SendBTState(BattleAllyState state)
        {
            TrySendBTState(state);
        }

        public bool TrySendBTState(BattleAllyState state)
        {
            if (!TryResolveStateChannel())
            {
                Debug.LogWarning($"[BattleAlly] {name} could not send BT state {state}: state channel is missing.", this);
                return false;
            }

            _stateChannel.SendEventMessage(state);
            return true;
        }

        protected override void Die()
        {
            IsDead = true;

            if (_allyManager != null)
                _allyManager.Unregister(this);
        }

        protected virtual void OnDestroy()
        {
            Bus<BattleEnd>.OnEvent -= HandleBattleEnd;
            UnsubscribeHealth();

            if (_health != null)
                _health.OnDeathSequenceStarted -= HandleDeathSequenceStarted;

            if (_allyManager != null)
                _allyManager.Unregister(this);
        }

        private void HandleDeathSequenceStarted(YIS.Code.Defines.Elemental elemental)
        {
            DeadAnimRoute();
        }

        private IEnumerator RestoreHealthAfterInit()
        {
            while (_health == null || !_health.IsInitialized || _health.MaxHealth <= 0f)
                yield return null;

            string allyId = GetAllyId();
            AllyPartyService service = ResolvePartyService();
            if (service != null && service.TryGetHealth(allyId, out float savedCurrent, out _))
            {
                float fixedCurrent = Mathf.Clamp(savedCurrent, 0f, _health.MaxHealth);
                _health.SetCurrentHealthSilently(fixedCurrent);

                if (fixedCurrent <= 0f)
                    DeadAnimRoute();
            }
            else
            {
                service?.SaveHealth(allyId, _health.CurrentHealth, _health.MaxHealth);
            }

            _healthRestored = true;
            TrySubscribeHealth();
        }

        private void TrySubscribeHealth()
        {
            if (_healthSubscribed || _health == null)
                return;

            _health.OnHealthChangeEvent += HandleHealthChanged;
            _healthSubscribed = true;
        }

        private void UnsubscribeHealth()
        {
            if (!_healthSubscribed || _health == null)
                return;
            
            _health.OnHealthChangeEvent -= HandleHealthChanged;
            _healthSubscribed = false;
        }
        
        private void HandleHealthChanged(float current, float max)
        {
            if (!_healthRestored)
                return;
            
            ResolvePartyService()?.SaveHealth(GetAllyId(), current, max);
        }
        
        private void HandleBattleEnd(BattleEnd evt)
        {
            if (_health != null && _healthRestored)
                ResolvePartyService()?.SaveHealth(GetAllyId(), _health.CurrentHealth, _health.MaxHealth);
        }
        
        private IEnumerator ApplyRewardHealthAfterInit(AllyRewardResult result)
        {
            while (_health == null || !_health.IsInitialized || _health.MaxHealth <= 0f)
                yield return null;
            
            ApplyCurrentHealth(result.CurrentHp);
        }
        
        private void ApplyCurrentHealth(float currentHp)
        {
            if (_health == null)
                return;
            
            float fixedCurrent = Mathf.Clamp(currentHp, 0f, _health.MaxHealth);
            _health.SetCurrentHealth(fixedCurrent);
        }
        
        private string GetAllyId()
        {
            if (allySO != null && !string.IsNullOrWhiteSpace(allySO.AllyId))
                return allySO.AllyId;
            
            return name;
        }
        
        private AllyPartyService ResolvePartyService()
        {
            if (_partyService != null)
                return _partyService;
            
            if (AllyPartyRepository.Instance != null)
            {
                _partyService = AllyPartyRepository.Instance.Service;
                return _partyService;
            }
            
            AllyPartyRepository repository = FindAnyObjectByType<AllyPartyRepository>(FindObjectsInactive.Include);
            _partyService = repository != null ? repository.Service : null;
            return _partyService;
        }
        
        private bool TryResolveStateChannel()
        {
            if (_stateChannel != null)
                return true;
            
            if (BtAgent == null)
                BtAgent = GetComponent<BehaviorGraphAgent>();
            
            if (BtAgent == null)
                return false;
            
            BlackboardVariable<ChangeAllyState> bbVar =
                BtAgent.GetVariable(StateChannelKey, out BlackboardVariable<ChangeAllyState> result) ? result : null;
            _stateChannel = bbVar?.Value;
            return _stateChannel != null;
        }
        
    }
}
