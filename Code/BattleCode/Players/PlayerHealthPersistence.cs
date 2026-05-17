using System.Collections;
using CIW.Code.System.Events;
using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.InputSystem;
using YIS.Code.Effects;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Players
{
    public class PlayerHealthPersistence : MonoBehaviour, IModule
    {
        [SerializeField] private PoolItemSO healEffectPrefab;
     
        [Inject] private PoolManagerMono _poolManager;
        
        private EntityHealth _health;

        private bool _hasSaved;
        private float _savedCur;
        private float _savedMax;

        private bool _restoreApplied;
        private bool _subscribed;

        public void Initialize(ModuleOwner owner)
        {
            _health ??= owner.GetModule<EntityHealth>();
        }

        private void Awake()
        {
            _health ??= GetComponent<EntityHealth>() ?? GetComponentInChildren<EntityHealth>(true);
        }

        private void OnEnable()
        {
            Bus<BattleEnd>.OnEvent += OnBattleEnd;
            PlayerHealthSave.OnResetRequested += HandleResetRequested;
            Bus<HealRequest>.OnEvent += OnHealRequest;

            _hasSaved = PlayerHealthSave.TryLoad(out _savedCur, out _savedMax);
            _restoreApplied = false;

            StartCoroutine(RestoreAfterInit());
            TrySubscribe();
        }

        private void Start()
        {
            if (_health != null)
            {
                PlayerHealthSave.SaveSnapshot(_health.CurrentHealth, _health.MaxHealth);
            }
        }
        
        private void OnDisable()
        {
            Bus<BattleEnd>.OnEvent -= OnBattleEnd;
            PlayerHealthSave.OnResetRequested -= HandleResetRequested;
            Bus<HealRequest>.OnEvent -= OnHealRequest;

            if (_health != null && _subscribed)
            {
                _health.OnHealthChangeEvent -= OnHealthChanged;
                _subscribed = false;
            }
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (_health == null) return;

            _health.OnHealthChangeEvent += OnHealthChanged;
            _subscribed = true;
        }
        
        private void Update()
        {
#if UNITY_EDITOR
            
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                PlayerHealthSave.Reset();
                
                if (_health != null && _health.IsInitialized)
                {
                    _health.SetCurrentHealth(_health.MaxHealth);
                    PlayerHealthSave.SaveSnapshot(_health.CurrentHealth, _health.MaxHealth);
                }
            }
#endif
            if (Keyboard.current.f4Key.wasPressedThisFrame)
            {
                Bus<HealRequest>.Raise(new HealRequest(0.2f, HealMode.MaxPercent));
            }
        }
        

        private void HandleResetRequested()
        {
            StartCoroutine(ResetAfterInit());
        }

        private IEnumerator ResetAfterInit()
        {
            while (_health == null || !_health.IsInitialized || _health.MaxHealth <= 0f)
                yield return null;

            bool prev = _restoreApplied;
            _restoreApplied = false;

            _health.SetCurrentHealth(_health.MaxHealth);
            PlayerHealthSave.SaveSnapshot(_health.CurrentHealth, _health.MaxHealth);

            _restoreApplied = prev;
        }
        
        private IEnumerator RestoreAfterInit()
        {
            while (_health == null || !_health.IsInitialized || _health.MaxHealth <= 0f)
                yield return null;

            if (_hasSaved)
            {
                float fixedCur = Mathf.Clamp(_savedCur, 0f, _health.MaxHealth);
                _health.SetCurrentHealth(fixedCur);
                PlayerHealthSave.SaveSnapshot(fixedCur, _health.MaxHealth);
            }
            else
            {
                PlayerHealthSave.SaveSnapshot(_health.CurrentHealth, _health.MaxHealth);
            }

            _restoreApplied = true;
        }
        
        private void OnHealthChanged(float current, float max)
        {
            if (!_restoreApplied) return;
            PlayerHealthSave.SaveSnapshot(current, max);
        }

        private void OnBattleEnd(BattleEnd evt)
        {
            if (evt.IsVictory)
                PlayerHealthSave.Flush();
        }
        
        private void OnHealRequest(HealRequest req)
        {
            float beforeHealth = _health.CurrentHealth;

            _health.Heal(req.value, req.mode);
            
            float actualHealed = _health.CurrentHealth - beforeHealth;
            if (actualHealed <= 0f) return;
            
            Bus<HealTextUiEvent>.Raise(new HealTextUiEvent(transform.position, actualHealed));
            
            Vector3 targetPos = transform.position;
            Quaternion rot = transform.rotation;
            
            PoolAnimatorEffect p = _poolManager.Pop<PoolAnimatorEffect>(healEffectPrefab);
            
            EffectTrigger evt = p.GetComponentInChildren<EffectTrigger>();
            evt.OnEndTrigger += () => p.DestroyObj();
            
            p.transform.SetParent(transform);
            p.transform.localScale = Vector3.one * 0.7f;
            p.PlayClipEffect(targetPos, rot, Animator.StringToHash("SLASH"));
        }
        
    }
}
