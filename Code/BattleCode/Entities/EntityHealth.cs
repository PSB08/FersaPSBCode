using System;
using System.Collections.Generic;
using CIW.Code;
using Code.Scripts.Entities;
using PSB.Code.BattleCode.Players;
using PSB_Lib.StatSystem;
using PSB.Code.BattleCode.Events;
using PSW.Code.Battle;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.InputSystem;
using YIS.Code.Combat;
using YIS.Code.Defines;
using YIS.Code.Events;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Entities
{
    public class EntityHealth : MonoBehaviour, IModule, IDamageable, IHealable
    {
        private Entity _entity;
        private EntityStat _statCompo;

        [SerializeField] private StatSO hpStat;
        [SerializeField] private float maxHealth;
        [SerializeField] private float currentHealth;

        [SerializeField] private HpUI_Controller[] hpController;

        public Dictionary<object, float> DamagedMultiplierModifiers = new Dictionary<object, float>(); // 받는 피해 증가량 변동치 모음
        public delegate void HealthChange(float current, float max);
        public event HealthChange OnHealthChangeEvent;
        public event HealthChange OnTotalHealthChangeEvent;

        private List<float> _phaseBounds = new List<float>();
        private int _currentPhaseIndex = 0;
        private int _lastVisualPhaseIndex = -1;
        private bool _isPhaseMode = false;
        private float _damageMultiplier = 1; // 받는 피해 증가량 - 기본 1

        public float CurrentHealth 
        {
            get 
            {
                if (_isPhaseMode && _phaseBounds.Count > 1 && maxHealth > 0)
                {
                    float bottom = _phaseBounds[_currentPhaseIndex + 1];
                    return Mathf.Max(0, currentHealth - maxHealth * bottom);
                }
                return currentHealth;
            }
        }
        
        public float MaxHealth 
        {
            get 
            {
                if (_isPhaseMode && _phaseBounds.Count > 1 && maxHealth > 0)
                {
                    float top = _phaseBounds[_currentPhaseIndex];
                    float bottom = _phaseBounds[_currentPhaseIndex + 1];
                    return maxHealth * top - maxHealth * bottom;
                }
                return maxHealth;
            }
        }
        
        public bool IsInitialized { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            _entity = owner as Entity;
            _statCompo = owner.GetModule<EntityStat>();
        }

        private void Start()
        {
            currentHealth = maxHealth = _statCompo.SubscribeStat(hpStat, HandleMaxHpChange, 10f);

            IsInitialized = true;

            if (!_isPhaseMode)
            {
                if (hpController != null)
                {
                    foreach (var controller in hpController)
                    {
                        controller.Init(currentHealth, maxHealth, _entity is BattlePlayer);
                        controller.ChangeCurrentHp(currentHealth);
                    }
                }
            }
            else
            {
                UpdatePhaseUI();
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
            _statCompo.UnSubscribeStat(hpStat, HandleMaxHpChange);
        }

        public void SetPhaseThresholds(List<float> thresholds)
        {
            _phaseBounds.Clear();
            _phaseBounds.Add(1f);
            
            if (thresholds != null && thresholds.Count > 0)
            {
                _isPhaseMode = true;
                foreach(var t in thresholds)
                {
                    if (t < 1f && t > 0f) _phaseBounds.Add(t);
                }
            }
            else
            {
                _isPhaseMode = false;
            }
            
            _phaseBounds.Add(0f);
            _currentPhaseIndex = 0;
            _lastVisualPhaseIndex = -1;
            
            if (_isPhaseMode)
            {
                UpdatePhaseUI();
            }
        }

        private void UpdatePhaseUI()
        {
            if (!_isPhaseMode || maxHealth <= 0) return;
            
            float currentPercent = currentHealth / maxHealth;

            while (_currentPhaseIndex < _phaseBounds.Count - 2 && currentPercent <= _phaseBounds[_currentPhaseIndex + 1])
            {
                _currentPhaseIndex++;
            }

            float currentPhaseMaxLimit = maxHealth * _phaseBounds[_currentPhaseIndex];
            if (currentHealth > currentPhaseMaxLimit)
            {
                currentHealth = currentPhaseMaxLimit;
            }

            if (hpController != null && _phaseBounds.Count > 1)
            {
                float top = _phaseBounds[_currentPhaseIndex];
                float bottom = _phaseBounds[_currentPhaseIndex + 1];

                float chunkMaxHp = maxHealth * top - maxHealth * bottom;
                float chunkCurrentHp = currentHealth - maxHealth * bottom;
                
                chunkCurrentHp = Mathf.Max(0, chunkCurrentHp);

                if (_lastVisualPhaseIndex != _currentPhaseIndex)
                {
                    foreach (var controller in hpController)
                        controller.Init(chunkCurrentHp, chunkMaxHp, _entity is BattlePlayer);
                    _lastVisualPhaseIndex = _currentPhaseIndex;
                }
                else
                {
                    foreach (var controller in hpController)
                    {
                        controller.ChangeMaxHp(chunkMaxHp);
                        controller.ChangeCurrentHp(chunkCurrentHp);
                    }
                }
            }
        }

        private void HandleMaxHpChange(StatSO stat, float currentValue, float prevValue)
        {
            float changed = currentValue - prevValue;
            maxHealth = currentValue;

            if (changed > 0)
            {
                currentHealth = Mathf.Max(0, currentHealth + changed);
            }
            else
            {
                currentHealth = Mathf.Max(0, currentHealth);
            }
            
            if (_isPhaseMode)
            {
                UpdatePhaseUI();
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                if (hpController != null)
                {
                    foreach (var controller in hpController)
                        controller.Init(currentHealth, maxHealth, _entity is BattlePlayer);
                }
            }

            InvokeHealthChangeEvent();
            OnTotalHealthChangeEvent?.Invoke(currentHealth, maxHealth);
        }

        public void ApplyDamage(DamageData damageData)
        {
            float damage = damageData.Damage * _damageMultiplier;
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            if (_isPhaseMode)
            {
                UpdatePhaseUI();
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                if (hpController != null)
                {
                    foreach (var controller in hpController)
                        controller.ChangeCurrentHp(currentHealth);
                }
            }

            InvokeHealthChangeEvent();
            OnTotalHealthChangeEvent?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Bus<EntityDeadElementalEvent>.Raise(new EntityDeadElementalEvent(gameObject, damageData.ElementalType));
                _entity.OnDeadEvent?.Invoke();
            }
            else
            {
                _entity.OnHitEvent?.Invoke();
            }
            
            Bus<DmgTextUiEvent>.Raise(new DmgTextUiEvent(_entity.transform.position, damage, damageData.ElementalType));
        }

        public void SetCurrentHealth(float value)
        {
            currentHealth = Mathf.Max(0, value);
            
            if (_isPhaseMode)
            {
                UpdatePhaseUI();
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
                if (hpController != null)
                {
                    foreach (var controller in hpController)
                        controller.ChangeCurrentHp(currentHealth);
                }
            }

            InvokeHealthChangeEvent();
            OnTotalHealthChangeEvent?.Invoke(currentHealth, maxHealth);
        }

        private void InvokeHealthChangeEvent()
        {
            if (_isPhaseMode && _phaseBounds.Count > 1 && maxHealth > 0)
            {
                float top = _phaseBounds[_currentPhaseIndex];
                float bottom = _phaseBounds[_currentPhaseIndex + 1];
                float chunkMaxHp = maxHealth * top - maxHealth * bottom;
                float chunkCurrentHp = Mathf.Max(0, currentHealth - maxHealth * bottom);
                
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
            float roundedAmount = MathF.Round(amount);
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
        

        public void AddDamagedMutiplierModifier(object key, float value)
        {
            if (DamagedMultiplierModifiers.ContainsKey(key)) return;
            _damageMultiplier += value;

            DamagedMultiplierModifiers.Add(key, value);
            Debug.Log($"[EntityHealth] {key}로 인해 {_entity.name}의 피해 증가량이 {value}만큼 증가. 현재 총 피해 증가량 : {_damageMultiplier}");
        }

        public void RemoveDamageMultiplierModifier(object key)
        {
            if (!DamagedMultiplierModifiers.ContainsKey(key)) return;
            _damageMultiplier -= DamagedMultiplierModifiers[key];

            DamagedMultiplierModifiers.Remove(key);
            Debug.Log($"[EntityHealth] {key}로 인해 {_entity.name}의 피해 증가량이 {DamagedMultiplierModifiers[key]}만큼 감소. 현재 총 피해 증가량 : {_damageMultiplier}");
        }
    }
}

public enum HealMode
{
    Flat,
    CurrentPercent,
    MaxPercent
}