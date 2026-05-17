using System;
using System.Collections;
using CIW.Code;
using Code.Scripts.Entities;
using PSB.Code.BattleCode.Players;
using PSB_Lib.StatSystem;
using PSB.Code.BattleCode.Events;
using PSW.Code.Battle;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.InputSystem;
using YIS.Code.UI;
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

        [SerializeField] private HpUI_Controller hpController;

        public delegate void HealthChange(float current, float max);
        public event HealthChange OnHealthChangeEvent;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        
        public bool IsInitialized { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            _entity = owner as Entity;
            _statCompo = owner.GetModule<EntityStat>();
        }

        private void Start()
        {
            currentHealth = maxHealth = _statCompo.SubscribeStat(hpStat, HandleMaxHpChange, 10f);

            if (hpController != null)
            {
                hpController.Init(currentHealth, maxHealth, _entity is BattlePlayer);
                hpController.ChangeCurrentHp(currentHealth);
            }

            IsInitialized = true;
            OnHealthChangeEvent?.Invoke(currentHealth, maxHealth);
        }

        //#if UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current.f5Key.wasPressedThisFrame)
                ApplyDamage(new DamageData(20, Elemental.Normal));
        }
        //#endif

        private void OnDestroy()
        {
            _statCompo.UnSubscribeStat(hpStat, HandleMaxHpChange);
        }

        private void HandleMaxHpChange(StatSO stat, float currentValue, float prevValue)
        {
            float changed = currentValue - prevValue;
            maxHealth = currentValue;

            if (changed > 0)
            {
                currentHealth = Mathf.Clamp(currentHealth + changed, 0, maxHealth);
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            }
            
            OnHealthChangeEvent?.Invoke(currentHealth, maxHealth);
            if (hpController != null)
                hpController.Init(currentHealth, maxHealth, _entity is BattlePlayer);
        }

        public void ApplyDamage(DamageData damageData)
        {
            float damage = damageData.Damage;
            currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
            OnHealthChangeEvent?.Invoke(currentHealth, maxHealth);

            if (hpController != null)
                hpController.ChangeCurrentHp(currentHealth);

            if (currentHealth <= 0)
            {
                Bus<EntityDeadElementalEvent>.Raise(new EntityDeadElementalEvent
                    (gameObject, damageData.ElementalType));
                _entity.OnDeadEvent?.Invoke();
            }
            else
            {
                _entity.OnHitEvent?.Invoke();
            }
            
            Bus<DmgTextUiEvent>.Raise(new DmgTextUiEvent((Vector2)_entity.transform.position, damage, damageData.ElementalType ));
        }

        public void SetCurrentHealth(float value)
        {
            currentHealth = Mathf.Clamp(value, 0, maxHealth);

            OnHealthChangeEvent?.Invoke(currentHealth, maxHealth);
            if (hpController != null) hpController.ChangeCurrentHp(currentHealth);
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
        
    }
}

public enum HealMode
{
    Flat,
    CurrentPercent,
    MaxPercent
}

