using System;
using CIW.Code;
using Code.Scripts.Entities;
using PSB_Lib.StatSystem;
using UnityEngine;
using UnityEngine.Events;
using YIS.Code.Modules;

namespace Code.Scripts.Enemies.BT
{
    public class AgentMovement : MonoBehaviour, IModule, IAfterInitModule
    {
        [field: SerializeField] public Rigidbody2D RigidCompo { get; private set; }
        public Vector2 Velocity => RigidCompo.linearVelocity;
        public bool CanManualMove { get; set; } = true;
        
        [field: SerializeField] public AnimParamSO VelocityParam { get; private set; }
        public UnityEvent<int, float> OnSpeedParamChange;
        public UnityEvent<float> OnXMoveChange;

        [SerializeField] private StatSO moveSpeedStat;
        [SerializeField] private float moveSpeed = 5f;
        
        private EntityStat _statCompo;
        
        private Entity _entity;
        private Vector2 _moveInput;
        private float _moveSpeedMultiplier = 1f;
        
        public void Initialize(ModuleOwner owner)
        {
            _entity = owner as Entity;
            _statCompo = owner.GetModule<EntityStat>();
            _moveSpeedMultiplier = 1f;
        }

        public void SetMoveSpeedMultiplier(float value)
        {
            _moveSpeedMultiplier = value;
        }

        public void AfterInitialize()
        {
            moveSpeed = _statCompo.SubscribeStat(moveSpeedStat, HandleMoveSpeedChange, 10f);
        }

        private void OnDestroy()
        {
            _statCompo.UnSubscribeStat(moveSpeedStat, HandleMoveSpeedChange);
        }

        private void HandleMoveSpeedChange(StatSO stat, float currentValue, float prevValue)
        {
            moveSpeed = currentValue;
        }
        
        public void StopImmediately()
        {
            _moveInput = Vector2.zero;
            RigidCompo.linearVelocity = Vector2.zero;
        }

        public void SetMovement(Vector2 input)
        {
            _moveInput = input;
        }

        private void FixedUpdate()
        {
            if (!CanManualMove)
            {
                _moveInput = Vector2.zero;
                if (RigidCompo.linearVelocity.sqrMagnitude > 0.0001f)
                    RigidCompo.linearVelocity = Vector2.zero;
                
                if (VelocityParam != null)
                    OnSpeedParamChange?.Invoke(VelocityParam.paramHash, 0f);

                OnXMoveChange?.Invoke(0f);
                return;
            }

            if (CanManualMove)
            {
                float xMove = Mathf.Approximately(RigidCompo.linearVelocityX,0) ? 
                    0 : Mathf.Sign(RigidCompo.linearVelocityX);
                RigidCompo.linearVelocity = _moveInput * (moveSpeed * _moveSpeedMultiplier);
                OnXMoveChange?.Invoke(xMove);
            }
            
            if (VelocityParam != null)
            {
                float velocity = RigidCompo.linearVelocity.magnitude;
                OnSpeedParamChange?.Invoke(VelocityParam.paramHash, velocity);
            }
        }

        public void AddForceToEntity(Vector2 force)
        {
            RigidCompo.AddForce(force, ForceMode2D.Impulse);
        }
        
    }
}