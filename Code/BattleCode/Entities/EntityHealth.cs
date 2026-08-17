using System;
using System.Collections.Generic;
using CIW.Code;
using CIW.Code.System.Events;
using Code.Scripts.Entities;
using PSB.Code.BattleCode.Players;
using PSB_Lib.StatSystem;
using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.UIs;
using PSW.Code.Battle;
using PSW.Code.EventBus;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using YIS.Code.Combat;
using YIS.Code.Defines;
using YIS.Code.Events;
using YIS.Code.Modules;
using Work.CSH.Scripts.PlayerComponents;

namespace PSB.Code.BattleCode.Entities
{
    public class EntityHealth : MonoBehaviour, IModule, IDamageable, IHealable
    {
        private Entity _entity;
        private EntityStat _statCompo;
        
        [SerializeField] private StatSO hpStat;
        [SerializeField] private float maxHealth;
        [SerializeField] private float currentHealth;
        [SerializeField] private float currentShield;
        
        [SerializeField] private HpUI_Controller[] hpController;
        
        private readonly Dictionary<object, float> _shieldGainModifiers = new Dictionary<object, float>();
        private float _shieldGainModifier;
        
        public Dictionary<object, float> DamagedMultiplierModifiers = new Dictionary<object, float>(); // 받는 피해 증가량 변동치 모음
        public HashSet<object> UndyingModifiers = new HashSet<object>();
        
        public delegate void HealthChange(float current, float max);
        public event HealthChange OnHealthChangeEvent;
        public event HealthChange OnTotalHealthChangeEvent;
        public event Action<float> OnShieldChangeEvent;
        public event Action OnUndyingTriggeredEvent;
        
        private List<float> _phaseBounds = new List<float>();
        private int _currentPhaseIndex = 0;
        private int _lastVisualPhaseIndex = -1;
        private bool _isPhaseMode = false;
        
        //페이즈 경계에 닿았지만 Wake 연출 전이라 UI 페이즈 넘김을 미뤄둔 상태
        private bool _pendingPhaseAdvance = false;
        private bool _deathSequenceStarted = false;
        private float _damagedMultiplier = 1; // 받는 피해 증가량 - 기본 1
        
        //외부 시스템이 지금 페이즈 전환을 보류해야 하는지 EntityHealth에 알려주는 콜백
        private Func<bool> _phaseHold;
        
        public float CurrentHealth 
        {
            get 
            {
                if (_isPhaseMode && _phaseBounds.Count > 1 && maxHealth > 0)
                {
                    return GetPhaseCurrentHealth();
                }
                
                return Mathf.RoundToInt(currentHealth);
            }
        }
        
        public float MaxHealth 
        {
            get 
            {
                if (_isPhaseMode && _phaseBounds.Count > 1 && maxHealth > 0)
                {
                    return GetPhaseMaxHealth();
                }
                
                return Mathf.RoundToInt(maxHealth);
            }
        }
        
        public float TotalCurrentHealth => Mathf.Max(0, Mathf.RoundToInt(currentHealth));
        public float TotalMaxHealth => Mathf.Max(0, Mathf.RoundToInt(maxHealth));
        public float CurrentShield => Mathf.Max(0, Mathf.RoundToInt(currentShield));
        
        public bool IsInitialized { get; private set; }  //죽음 처리 중 HP UI 0 애니메이션이 끝나기 전까지 실제 삭제를 막는 플래그
        public bool CanFinishDeathSequence { get; private set; } = true; //현재 페이즈 UI 전환이 Wake 연출까지 보류되어 있는지 외부에서 확인하는 값
        public bool HasPendingPhase => _pendingPhaseAdvance; //현재 페이즈 UI 전환이 Wake 연출까지 보류되어 있는지 외부에서 확인하는 값
        public bool RaiseDeathElementalEventOnDeathStart { get; set; } //HP UI 완료 전에도 죽음 애니메이션을 바로 시작시키기 위한 이벤트
        public event Action<Elemental> OnDeathSequenceStarted;
        
        public void Initialize(ModuleOwner owner)
        {
            _entity = owner as Entity;
            _statCompo = owner.GetModule<EntityStat>();
            currentShield = 0f;
        }
        
        private void OnEnable()
        {
            Bus<BattleEnd>.OnEvent += HandleBattleEnded;
        }
        
        private void OnDisable()
        {
            Bus<BattleEnd>.OnEvent -= HandleBattleEnded;
        }
        
        private void HandleBattleEnded(BattleEnd evt)
        {
            currentShield = 0f;
            
            if (IsInitialized)
                RefreshShieldUI();
        }
        
        private void Start()
        {
            maxHealth = Mathf.RoundToInt(_statCompo.SubscribeStat(hpStat, HandleMaxHpChange, 10f));
            currentHealth = maxHealth;
            NormalizeHealth();
            
            IsInitialized = true;
            
            if (!_isPhaseMode)
            {
                if (hpController != null)
                {
                    foreach (var controller in hpController)
                    {
                        controller.Init(currentHealth, maxHealth, currentShield, _entity is BattlePlayer);
                    }
                }
            }
            else
            {
                RefreshPhaseUI();
            }
            
            InvokeHealthChangeEvent();
            OnTotalHealthChangeEvent?.Invoke(currentHealth, maxHealth);
        }
        
#if UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current.f5Key.wasPressedThisFrame)
                ApplyDamage(new DamageData(20, Elemental.Normal));
        }
#endif
        
        private void OnDestroy()
        {
            if (_statCompo != null)
                _statCompo.UnSubscribeStat(hpStat, HandleMaxHpChange);
        }
        
        public void SetPhaseThresholds(List<float> thresholds)
        {
            _phaseBounds.Clear();
            _phaseBounds.Add(1f);
            
            if (thresholds != null && thresholds.Count > 0)
            {
                _isPhaseMode = true;
                
                foreach(var t in thresholds.Distinct().OrderByDescending(x => x))
                {
                    if (t < 1f && t > 0f)
                        _phaseBounds.Add(t);
                }
            }
            else
            {
                _isPhaseMode = false;
            }
            
            _phaseBounds.Add(0f);
            _currentPhaseIndex = 0;
            _lastVisualPhaseIndex = -1;
            _pendingPhaseAdvance = false;
            
            if (_isPhaseMode)
            {
                RefreshPhaseUI();
            }
        }
        
        public void SetPhaseHold(Func<bool> predicate)
        {
            //외부 컨트롤러가 페이즈 UI 전환을 잠시 붙잡을 수 있게 연결
            _phaseHold = predicate;
        }
        
        public void ClearPhaseHold(Func<bool> predicate)
        {
            //등록한 쪽과 같은 콜백일 때만 제거해서 다른 시스템 콜백을 실수로 지우지 않게
            if (_phaseHold == predicate)
                _phaseHold = null;
        }
        
        private void RefreshPhaseUI(bool animateEntry = false, Action onHpDone = null, 
            bool allowAdvance = true, bool forceAdvance = false, bool immediate = false)
        {
            //페이즈 체력 모드가 아니면 페이즈 UI 계산을 하지 않음
            if (!_isPhaseMode || maxHealth <= 0)
            {
                onHpDone?.Invoke();
                return;
            }
            
            //계산 전에 체력을 정수, 범위 안으로 맞추기
            NormalizeHealth();
            
            //전체 체력을 기준으로 현재 페이즈 경계를 넘었는지 확인
            int totalCurrentHealth = Mathf.RoundToInt(currentHealth);
            //UI 페이즈가 실제로 바뀌었는지 비교하기 위해 이전 인덱스를 저장
            int previousPhaseIndex = _currentPhaseIndex;
            
            if (forceAdvance && _currentPhaseIndex < _phaseBounds.Count - 2)
            {
                //Wake에서 보류된 페이즈 전환을 해제할 때는 MaxHP 증가로 체력이 경계보다 높아져도 다음 UI 페이즈로 넘김
                _currentPhaseIndex++;
            }
            else
            {
                //allowAdvance가 true일 때만 다음 페이즈 인덱스로 이동
                while (allowAdvance && _currentPhaseIndex < _phaseBounds.Count - 2 &&
                       totalCurrentHealth <= GetPhaseBoundHealth(_currentPhaseIndex + 1))
                {
                    //현재 체력이 다음 경계 이하라면 UI가 보여줄 페이즈 인덱스를 하나 넘기기
                    _currentPhaseIndex++;
                }
            }
            
            //이번 RefreshPhaseUI 호출에서 페이즈 인덱스가 바뀌었는지 기록
            bool phaseChanged = previousPhaseIndex != _currentPhaseIndex;
            
            //현재 페이즈의 상한 체력보다 실제 체력이 높으면 상한으로 잘라내기
            int phaseTop = GetPhaseBoundHealth(_currentPhaseIndex);
            if (currentHealth > phaseTop)
            {
                //페이즈 UI 계산이 틀어지지 않게 실제 체력도 현재 페이즈 상한 안에 맞춤
                currentHealth = phaseTop;
                NormalizeHealth();
            }
            
            //체력 UI 컨트롤러가 있으면 현재 페이즈 기준 체력으로 UI를 갱신
            if (hpController != null && _phaseBounds.Count > 1)
            {
                float chunkMaxHp = GetPhaseMaxHealth();
                float chunkCurrentHp = GetPhaseCurrentHealth();
                
                //UI를 새 페이즈 기준으로 Init
                if (_lastVisualPhaseIndex != _currentPhaseIndex || immediate)
                {
                    InitHpBars(chunkCurrentHp, chunkMaxHp, !immediate && animateEntry && phaseChanged, onHpDone);
                    
                    //같은 페이즈에서 반복 Init하지 않게 마지막 표시 페이즈를 저장
                    _lastVisualPhaseIndex = _currentPhaseIndex;
                }
                else
                {
                    //같은 페이즈 안에서는 최대 체력만 갱신한 뒤 현재 체력 애니메이션을 실행
                    foreach (var controller in hpController)
                    {
                        controller.ChangeMaxHp(chunkMaxHp);
                    }
                    
                    SetHpBars(chunkCurrentHp, onHpDone);
                }
            }
            else
            {
                //UI 컨트롤러가 없으면 대기할 애니메이션이 없으므로 완료 콜백을 바로 호출
                onHpDone?.Invoke();
            }
        }
        
        private void HandleMaxHpChange(StatSO stat, float currentValue, float prevValue)
        {
            float roundedCurrentValue = Mathf.RoundToInt(currentValue);
            float roundedPrevValue = Mathf.RoundToInt(prevValue);
            
            float changed = roundedCurrentValue - roundedPrevValue;
            maxHealth = roundedCurrentValue;
            
            if (changed > 0)
            {
                currentHealth += changed;
            }
            
            NormalizeHealth();
            
            if (_isPhaseMode)
            {
                //PhaseBreak 보류 중 MaxHP가 늘면 Wake에서 새 페이즈 UI를 채우기 전까지 중간 UI 갱신을 막음
                if (!_pendingPhaseAdvance)
                    RefreshPhaseUI();
            }
            else
            {
                if (hpController != null)
                {
                    foreach (var controller in hpController)
                        controller.Init(currentHealth, maxHealth, currentShield, _entity is BattlePlayer);
                }
            }
            
            InvokeHealthChangeEvent();
            OnTotalHealthChangeEvent?.Invoke(currentHealth, maxHealth);
        }
        
        public void ApplyDamage(DamageData damageData)
        {
            ApplyDamageInternal(damageData, true);
        }
        
        public void ApplyFixedDamage(DamageData damageData)
        {
            ApplyDamageInternal(damageData, false);
        }
        
        private void ApplyDamageInternal(DamageData damageData, bool applyDamageModifier)
        {
            if (_deathSequenceStarted || (_entity != null && _entity.IsDead))
                return;
            
            float previousHealth = currentHealth;
            float damageMultiplier = applyDamageModifier ? _damagedMultiplier : 1f;
            int intendedDamage = Mathf.Max(0, Mathf.RoundToInt(damageData.Damage * damageMultiplier));
            damageData.Damage = intendedDamage;
            
            int absorbedDamage = AbsorbShield(intendedDamage);
            int healthDamage = intendedDamage - absorbedDamage;
            
            currentHealth -= healthDamage;
            NormalizeHealth();
            
            //페이즈 경계를 넘어가는 피해는 다음 페이즈 체력까지 파고들지 않게 경계에서 멈춤
            bool phaseHit = ClampDamagePhase(previousHealth);
            bool holdPhase = phaseHit && ShouldHoldPhase();
            
            if (holdPhase)
            {
                //Stun, Wake 연출이 끝날 때까지 새 페이즈 HP UI 진입을 보류
                _pendingPhaseAdvance = true;
            }
            
            CameraEffectComponent cameraEffectCompo = _entity.GetModule<CameraEffectComponent>();
            Debug.Log($"[EntityHealth] {damageData.Damage}만큼 피해를 입음. " +
                      $"현재 체력 : {currentHealth}/{maxHealth}, 카메라compo : {cameraEffectCompo}");
            
            if (cameraEffectCompo != null)
            {
                cameraEffectCompo.DamagedShake();
            }
            
            bool undyingPreventedDeath = currentHealth <= 0 && UndyingModifiers.Count > 0;
            bool undyingTriggered = undyingPreventedDeath && previousHealth <= 1f;
            
            if (undyingPreventedDeath)
            {
                currentHealth = 1;
                
                if (undyingTriggered)
                {
                    RaiseUndyingLog();
                    OnUndyingTriggeredEvent?.Invoke();
                }
            }
            else
            {
                NormalizeHealth();
            }
            
            bool shouldDie = currentHealth <= 0;
            Elemental deathElemental = damageData.ElementalType;
            Action onDeathAnimComplete = null;
            bool shouldRaiseDeathComplete = true;
            
            if (shouldDie)
            {
                _deathSequenceStarted = true;
                CanFinishDeathSequence = false;
                _entity.IsDead = true;
                
                if (RaiseDeathElementalEventOnDeathStart)
                {
                    Bus<EntityDeadElementalEvent>.Raise(new EntityDeadElementalEvent(gameObject, deathElemental));
                    shouldRaiseDeathComplete = false;
                }
                
                //죽음 애니메이션은 HP UI가 0까지 닳는 애니메이션과 동시에 시작할 수 있게 먼저 알림
                OnDeathSequenceStarted?.Invoke(deathElemental);
                
                onDeathAnimComplete = () =>
                    FinishDeathSequence(deathElemental, shouldRaiseDeathComplete);
            }
            
            if (_isPhaseMode)
            {
                //보류 중인 페이즈 전환이면 여기서는 UI 페이즈를 넘기지 않고 현재 칸 0까지만 보여줌
                RefreshPhaseUI(phaseHit && !holdPhase, onDeathAnimComplete, !holdPhase);
            }
            else
            {
                SetHpBars(currentHealth, onDeathAnimComplete);
            }
            
            int actualHealthDamage = Mathf.Max(0, Mathf.RoundToInt(previousHealth - currentHealth));
            damageData.Damage = absorbedDamage + actualHealthDamage;
            
            InvokeHealthChangeEvent();
            OnTotalHealthChangeEvent?.Invoke(currentHealth, maxHealth);
            
            if (!shouldDie && !holdPhase)
            {
                _entity.OnHitEvent?.Invoke();
            }
            
            if (undyingTriggered)
            {
                DamageData undyingData = new DamageData(0f, Elemental.Normal)
                {
                    Info = "불사!"
                };
                
                Bus<DmgTextUiEvent>.Raise(new DmgTextUiEvent(_entity.transform.position, undyingData));
            }
            else
            {
                Bus<DmgTextUiEvent>.Raise(new DmgTextUiEvent(_entity.transform.position, damageData));
            }
        }
        
        public void ResolvePhaseAdvance(Action onHpDone = null)
        {
            //Wake 시 호출되어 새 페이즈 HP UI를 0에서 현재 체력까지
            if (!_pendingPhaseAdvance)
            {
                //보류된 페이즈 전환이 없으면 기다릴 일이 없으므로 완료 콜백만 호출
                onHpDone?.Invoke();
                return;
            }
            
            //이번 호출에서 보류 상태를 소비
            _pendingPhaseAdvance = false;
            
            if (!_isPhaseMode)
            {
                //페이즈 모드가 아니면 UI 전환 없이 완료 처리
                onHpDone?.Invoke();
                return;
            }
            
            //true로 호출해서 실제 UI 페이즈를 다음 단계로 넘김
            RefreshPhaseUI(true, () =>
            {
                //페이즈 UI가 넘어간 뒤 외부 체력 새 페이즈 체력을 알림
                InvokeHealthChangeEvent();
                //HP UI 채우기 애니메이션 완료를 PhaseBreakController에 알림
                onHpDone?.Invoke();
            }, true, true);
        }
        
        private void InitHpBars(float currentHp, float maxHp, bool fromZero, Action onDone = null)
        {
            RunHpBars((bar, done) =>
                bar.Init(currentHp, maxHp, currentShield, _entity is BattlePlayer, fromZero, done), onDone);
        }
        
        private void SetHpBars(float hp, Action onDone = null)
        {
            RunHpBars((bar, done) => bar.ChangeCurrentHp(hp, done), onDone);
        }
        
        private void RunHpBars(Action<HpUI_Controller, Action> run, Action onDone)
        {
            if (hpController == null || hpController.Length == 0)
            {
                onDone?.Invoke();
                return;
            }
            
            int left = hpController.Count(controller => controller != null);
            if (left <= 0)
            {
                onDone?.Invoke();
                return;
            }
            
            foreach (var controller in hpController)
            {
                if (controller == null) continue;
                run(controller, () =>
                {
                    left--;
                    if (left <= 0)
                        onDone?.Invoke();
                });
            }
        }
        
        private void FinishDeathSequence(Elemental deathElemental, bool raiseDeathElementalEvent)
        {
            //HP UI 0 애니메이션 완료 후 SelfDestroyAction이 삭제를 진행할 수 있게
            CanFinishDeathSequence = true;
            
            //사망 시작 때 속성 이벤트를 보내지 않은 경우 여기서 최종 사망 이벤트를 보냄
            if (raiseDeathElementalEvent)
                Bus<EntityDeadElementalEvent>.Raise(new EntityDeadElementalEvent(gameObject, deathElemental));
            
            //기존 Entity 사망 이벤트를 호출해서 실제 사망 후처리
            _entity.OnDeadEvent?.Invoke();
        }
        
        private bool ClampDamagePhase(float previousHealth)
        {
            //현재 페이즈가 끝났을 때 남은 데미지가 다음 페이즈로 넘어가지 않도록 보정
            if (!_isPhaseMode || _phaseBounds.Count <= 2 || maxHealth <= 0f)
                return false;
            
            if (_currentPhaseIndex >= _phaseBounds.Count - 2)
                return false;
            
            int nextPhaseBoundary = GetPhaseBoundHealth(_currentPhaseIndex + 1);
            if (previousHealth <= nextPhaseBoundary || currentHealth > nextPhaseBoundary)
                return false;
            
            currentHealth = nextPhaseBoundary;
            NormalizeHealth();
            return true;
        }
        
        private bool ShouldHoldPhase()
        {
            //등록된 콜백이 true를 돌려주면 이번 페이즈 UI 전환을 보류
            return _phaseHold?.Invoke() == true;
        }
        
        public int AddShield(float amount)
        {
            int gainedShield = Mathf.Max(0, Mathf.RoundToInt(amount + _shieldGainModifier));
            
            if (gainedShield <= 0)
                return 0;
            
            currentShield += gainedShield;
            RefreshShieldUI();
            
            return gainedShield;
        }
        
        public void AddShieldGainModifier(object key, float value)
        {
            if (key == null) return;
            
            if (_shieldGainModifiers.TryGetValue(key, out float previousValue))
                _shieldGainModifier -= previousValue;
            
            _shieldGainModifiers[key] = value;
            _shieldGainModifier += value;
        }
        
        public void RemoveShieldGainModifier(object key)
        {
            if (key == null) return;
            if (!_shieldGainModifiers.TryGetValue(key, out float value)) return;
            
            _shieldGainModifier -= value;
            _shieldGainModifiers.Remove(key);
        }
        
        private int AbsorbShield(int damage)
        {
            if (damage <= 0 || currentShield <= 0f)
                return 0;
            
            int absorbedDamage = Mathf.Min(Mathf.RoundToInt(currentShield), damage);
            
            currentShield -= absorbedDamage;
            currentShield = Mathf.Max(0f, Mathf.RoundToInt(currentShield));
            RefreshShieldUI();
            
            return absorbedDamage;
        }
        
        private void RefreshShieldUI()
        {
            if (hpController != null)
            {
                foreach (HpUI_Controller controller in hpController)
                {
                    if (controller != null)
                        controller.ChangeShield(currentShield);
                }
            }
            
            OnShieldChangeEvent?.Invoke(CurrentShield);
        }
        
        public void SetCurrentHealth(float value)
        {
            currentHealth = value;
            NormalizeHealth();
            
            if (_isPhaseMode)
            {
                RefreshPhaseUI();
            }
            else
            {
                if (hpController != null)
                {
                    foreach (var controller in hpController)
                        controller.ChangeCurrentHp(currentHealth);
                }
            }
            
            InvokeHealthChangeEvent();
            OnTotalHealthChangeEvent?.Invoke(currentHealth, maxHealth);
        }
        
        public void SetCurrentHealthSilently(float value)
        {
            currentHealth = value;
            NormalizeHealth();
            
            if (_isPhaseMode)
            {
                RefreshPhaseUI(immediate: true);
            }
            else if (hpController != null)
            {
                foreach (var controller in hpController)
                    controller.Init(currentHealth, maxHealth, currentShield, _entity is BattlePlayer);
            }
            
            InvokeHealthChangeEvent();
            OnTotalHealthChangeEvent?.Invoke(currentHealth, maxHealth);
        }
        
        private void InvokeHealthChangeEvent()
        {
            if (_isPhaseMode && _phaseBounds.Count > 1 && maxHealth > 0)
            {
                float chunkMaxHp = GetPhaseMaxHealth();
                float chunkCurrentHp = GetPhaseCurrentHealth();
                
                OnHealthChangeEvent?.Invoke(chunkCurrentHp, chunkMaxHp);
            }
            else
            {
                OnHealthChangeEvent?.Invoke(currentHealth, maxHealth);
            }
        }
        
        [ContextMenu("DeathEnemy")]
        public void DeathEnemy()
        {
            ApplyDamage(new DamageData(1000, Elemental.Normal));
        }
        
        public void HealFlat(float amount)
        {
            if (amount <= 0f) return;
            
            float roundedAmount = Mathf.RoundToInt(amount);
            if (roundedAmount <= 0f) return;
            
            SetCurrentHealth(currentHealth + roundedAmount);
        }
        
        public void HealByCurrentPercent(float percent01)
        {
            if (percent01 <= 0f) return;
            float amount = currentHealth * percent01;
            HealFlat(amount);
        }
        
        public void HealByMaxPercent(float percent01)
        {
            if (percent01 <= 0f) return;
            float amount = maxHealth * percent01;
            HealFlat(amount);
        }
        
        public void Heal(float value, HealMode mode)
        {
            switch (mode)
            {
                case HealMode.Flat:
                    HealFlat(value);
                    break;
                
                case HealMode.CurrentPercent:
                    HealByCurrentPercent(value);
                    break;
                
                case HealMode.MaxPercent:
                    HealByMaxPercent(value);
                    break;
            }
        }
        
        private void NormalizeHealth()
        {
            maxHealth = Mathf.Max(0, Mathf.RoundToInt(maxHealth));
            currentHealth = Mathf.Clamp(Mathf.RoundToInt(currentHealth), 0, maxHealth);
        }
        
        private int GetPhaseCurrentHealth()
        {
            int totalCurrentHealth = Mathf.RoundToInt(currentHealth);
            if (totalCurrentHealth <= 0) return 0;
            
            int bottom = GetPhaseBoundHealth(_currentPhaseIndex + 1);
            int phaseCurrentHealth = totalCurrentHealth - bottom;
            
            return Mathf.Clamp(phaseCurrentHealth, 0, GetPhaseMaxHealth());
        }
        
        private int GetPhaseMaxHealth()
        {
            int top = GetPhaseBoundHealth(_currentPhaseIndex);
            int bottom = GetPhaseBoundHealth(_currentPhaseIndex + 1);
            
            return Mathf.Max(1, top - bottom);
        }
        
        private int GetPhaseBoundHealth(int boundIndex)
        {
            int roundedMaxHealth = Mathf.RoundToInt(maxHealth);
            
            if (boundIndex <= 0) return roundedMaxHealth;
            if (boundIndex >= _phaseBounds.Count - 1) return 0;
            
            return Mathf.Clamp(
                Mathf.FloorToInt(roundedMaxHealth * _phaseBounds[boundIndex]),
                0,
                roundedMaxHealth);
        }
        
        public void AddDamagedMultiplierModifier(object key, float value)
        {
            if (DamagedMultiplierModifiers.ContainsKey(key)) return;
            _damagedMultiplier += value;
            
            DamagedMultiplierModifiers.Add(key, value);
            Debug.Log($"[EntityHealth] {key}로 인해 {_entity.name}의 피해 증가량이 {value}만큼 증가. 현재 총 피해 증가량 : {_damagedMultiplier}");
        }
        
        public void RemoveDamageMultiplierModifier(object key)
        {
            if (!DamagedMultiplierModifiers.ContainsKey(key)) return;
            
            float value = DamagedMultiplierModifiers[key];
            _damagedMultiplier -= value;
            
            DamagedMultiplierModifiers.Remove(key);
            Debug.Log($"[EntityHealth] {key}로 인해 {_entity.name}의 피해 증가량이 {value}만큼 감소. " +
                      $"현재 총 피해 증가량 : {_damagedMultiplier}");
        }
        
        public void AddUndyingModifier(object key)
        {
            if (UndyingModifiers.Contains(key)) return;
            UndyingModifiers.Add(key);
            Debug.Log($"[EntityHealth] {key}로 인해 {_entity.name} 불사(체력 1 고정) 상태 획득.");
        }
        
        public void RemoveUndyingModifier(object key)
        {
            if (!UndyingModifiers.Contains(key)) return;
            UndyingModifiers.Remove(key);
            Debug.Log($"[EntityHealth] {key}로 인해 {_entity.name}의 불사 상태 해제.");
        }
        
        private void RaiseUndyingLog()
        {
            if (_entity == null) return;
            
            SystemLogOwner owner = _entity is BattlePlayer 
                ? SystemLogOwner.Player 
                : SystemLogOwner.Enemy;
            
            string targetName = SystemLogNameResolver.GetTargetName(_entity, owner, "플레이어");
            
            Bus<SystemLogEvent>.Raise(new SystemLogEvent($"{targetName}은/는 불사 효과로 쓰러지지 않았습니다.", owner));
        }
        
    }
}

public enum HealMode
{
    Flat,
    CurrentPercent,
    MaxPercent
}
