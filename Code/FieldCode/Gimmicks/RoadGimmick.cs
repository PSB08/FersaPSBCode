using System.Collections;
using System.Collections.Generic;
using CIW.Code;
using UnityEngine;
using Work.CSH.Scripts.PlayerComponents;

namespace Work.PSB.Code.FieldCode.Gimmicks
{
    public class RoadGimmick : FieldGimmick
    {
        [Header("Master System")]
        [SerializeField] private FieldGimmick masterGimmick;

        [Header("Damage Settings")]
        [SerializeField] private RoadDamageMode damageMode = RoadDamageMode.SingleHit;
        [SerializeField] private RoadActivationMode activationMode = RoadActivationMode.AlwaysOn;

        [Header("Timing")]
        [SerializeField] private float tickInterval = 1f;
        [SerializeField] private float blinkOnDuration = 2f;
        [SerializeField] private float blinkOffDuration = 2f;
        [SerializeField] private float blinkStartDelay = 0f;

        [Header("References")]
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private LayerMask targetLayer;

        private Collider2D _myCollider;
        private ContactFilter2D _contactFilter;
        private bool _isActiveTrap = true;
        private float _tickTimer = 0f;
        private Coroutine _blinkCoroutine;

        protected override void Awake()
        {
            base.Awake();
            _myCollider = GetComponent<Collider2D>();
            
            _contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = targetLayer,
                useTriggers = true
            };
        }

        protected override void Start()
        {
            base.Start();
            if (activationMode == RoadActivationMode.Blink)
            {
                _blinkCoroutine = StartCoroutine(BlinkRoutine());
            }
        }

        private void Update()
        {
            if (!_isActiveTrap || isCleared || damageMode != RoadDamageMode.ContinuousTick) 
                return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= tickInterval)
            {
                _tickTimer = 0f;
                ApplyDamageToOverlap();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActiveTrap || isCleared || damageMode != RoadDamageMode.SingleHit) 
                return;
            
            CheckAndApplyDamage(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!_isActiveTrap || isCleared || damageMode != RoadDamageMode.SingleHit) 
                return;
            
            CheckAndApplyDamage(other);
        }

        private void CheckAndApplyDamage(Collider2D other)
        {
            if ((targetLayer.value & (1 << other.gameObject.layer)) > 0)
            {
                var entity = other.GetComponentInParent<Entity>();
                
                if (entity != null && entity.gameObject.activeInHierarchy && entity is FieldPlayer)
                {
                    if (masterGimmick != null) 
                        masterGimmick.TriggerFail(entity);
                    else
                        OnFail(entity);
                }
            }
        }

        private void ApplyDamageToOverlap()
        {
            List<Collider2D> results = new List<Collider2D>();
            int count = Physics2D.OverlapCollider(_myCollider, _contactFilter, results);

            for (int i = 0; i < count; i++)
            {
                var entity = results[i].GetComponentInParent<Entity>();
                if (entity != null && entity.gameObject.activeInHierarchy && entity is FieldPlayer)
                {
                    if (masterGimmick != null) 
                        masterGimmick.TriggerFail(entity);
                    else 
                        OnFail(entity);
                }
            }
        }

        private IEnumerator BlinkRoutine()
        {
            if (blinkStartDelay > 0f) yield return new WaitForSeconds(blinkStartDelay);
            
            while (!isCleared)
            {
                SetTrapActive(true);
                yield return new WaitForSeconds(blinkOnDuration);

                SetTrapActive(false);
                yield return new WaitForSeconds(blinkOffDuration);
            }
        }

        private void SetTrapActive(bool active)
        {
            _isActiveTrap = active;
            
            if (visualRenderer != null) 
            {
                visualRenderer.enabled = active;
            }

            if (_isActiveTrap)
            {
                ApplyDamageToOverlap();
                _tickTimer = 0f;
            }
        }

        public override void ResetGimmick()
        {
            base.ResetGimmick();
            _tickTimer = 0f;
            
            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
            }

            if (activationMode == RoadActivationMode.Blink)
            {
                _blinkCoroutine = StartCoroutine(BlinkRoutine());
            }
            else
            {
                SetTrapActive(true);
            }
        }
        
    }
}