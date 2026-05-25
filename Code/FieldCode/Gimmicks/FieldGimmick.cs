using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.PSB.Code.FieldCode.MapSaves;
using YIS.Code.Combat;
using YIS.Code.Defines;

namespace Work.PSB.Code.FieldCode.Gimmicks
{
    public enum RoadDamageMode
    {
        SingleHit,
        ContinuousTick
    }

    public enum RoadActivationMode
    {
        AlwaysOn,
        Blink
    }
    
    public abstract class FieldGimmick : MonoBehaviour
    {
        [Header("Save Data")]
        [SerializeField] private string gimmickId;
        public string GimmickId => gimmickId;
        
        [SerializeField] private GameObject rewardChestPrefab;
        [SerializeField] private float failDamage = 10f;
        [SerializeField] private int maxAttempts = 3;
        [SerializeField] private Transform respawnPoint;
        
        [SerializeField] private float invincibilityDuration = 1.0f;
        
        public bool isCleared = false;
        private int _currentFails = 0;
        
        private static Dictionary<int, float> _entityHitTimes = new Dictionary<int, float>();

        protected virtual void Awake()
        {
            SceneObjectRegistry.RegisterGimmick(this);
        }

        protected virtual void OnDestroy()
        {
            SceneObjectRegistry.UnregisterGimmick(this);
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void Start()
        {
            if (rewardChestPrefab != null)
                rewardChestPrefab.SetActive(false);
        }
        
        public void RestoreClearedState()
        {
            isCleared = true;
            gameObject.SetActive(false);
        }

        public void TriggerFail(Entity playerEntity)
        {
            OnFail(playerEntity);
        }

        protected virtual void OnSuccess()
        {
            if (isCleared) return;
            isCleared = true;

            if (rewardChestPrefab != null)
            {
                rewardChestPrefab.SetActive(true);
            }

            gameObject.SetActive(false);
        }

        protected virtual void OnFail(Entity playerEntity)
        {
            if (isCleared || playerEntity == null) return;

            int entityId = playerEntity.GetInstanceID();
            float currentTime = Time.time;
            
            if (_entityHitTimes.TryGetValue(entityId, out float lastTime))
            {
                if (currentTime - lastTime < invincibilityDuration) return;
            }

            _entityHitTimes[entityId] = currentTime;

            var keysToRemove = new List<int>();
            foreach (var kvp in _entityHitTimes)
            {
                if (currentTime - kvp.Value > invincibilityDuration)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _entityHitTimes.Remove(key);
            }

            ApplyDamage(playerEntity);
            _currentFails++;

            TeleportToStart(playerEntity);
            
            if (maxAttempts > 0 && _currentFails >= maxAttempts)
            {
                OnPermanentFail();
            }
            else
            {
                ResetGimmick(); 
            }
        }

        private void ApplyDamage(Entity playerEntity)
        {
            var healthModule = playerEntity.GetModule<EntityHealth>();
            if (healthModule != null)
            {
                DamageData damageData = new DamageData(failDamage, Elemental.Normal);
                healthModule.ApplyDamage(damageData);
            }
        }

        private void TeleportToStart(Entity playerEntity)
        {
            if (respawnPoint != null)
            {
                playerEntity.transform.position = respawnPoint.position;
            }
        }

        protected virtual void OnPermanentFail()
        {
            if (rewardChestPrefab != null) Destroy(rewardChestPrefab);
            
            if (!string.IsNullOrEmpty(gimmickId))
            {
               SceneSaveSystem.SetGimmickCleared(
                    SceneManager.GetActiveScene().name, gimmickId, true);
            }

            Destroy(transform.parent != null ? transform.parent.gameObject : gameObject);
        }

        public virtual void ResetGimmick()
        {
            isCleared = false;
        }
        
    }
}