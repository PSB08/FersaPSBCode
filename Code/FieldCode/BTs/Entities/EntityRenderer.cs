using System;
using CIW.Code;
using Code.Scripts.Enemies.BT;
using UnityEngine;
using YIS.Code.Modules;

namespace Code.Scripts.Entities
{
    public class EntityRenderer : MonoBehaviour, IModule, IAnimator
    {
        [field: SerializeField]public float FacingDirection { get; private set; } = 1f;
        
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        public Animator Animator => animator;
        public SpriteRenderer SpriteRenderer => spriteRenderer;

        public Action OnAnimationHitTrigger;
        public Action OnAnimationEndTrigger;
        public Action OnDeadEndTrigger;
        
        private Entity _entity;
        private AnimParamSO _currClip;
        private bool _clipLocked;
        
        public void LockClip(bool locked) => _clipLocked = locked;

        public void ChangeClip(AnimParamSO nextClip)
        {
            if (_clipLocked) return;
            InternalChangeClip(nextClip);
        }

        public void ForceChangeClip(AnimParamSO nextClip)
        {
            InternalChangeClip(nextClip);
        }
        
        private void InternalChangeClip(AnimParamSO nextClip)
        {
            if (_currClip == nextClip) return;

            _currClip = nextClip;
            animator.Play(_currClip.paramHash, -1, 0f);
        }

        public bool HasParam(int hash)
        {
            foreach (var p in animator.parameters)
                if (p.nameHash == hash) return true;
            return false;
        }

        public void Initialize(ModuleOwner owner)
        {
            _entity = owner as Entity;
        }

        private void AnimationHit()
        {
            OnAnimationHitTrigger?.Invoke();
        }
        
        private void AnimationEnd()
        {
            OnAnimationEndTrigger?.Invoke();
        }
        
        private void OnDeadAnimationEnd()
        {
            OnDeadEndTrigger?.Invoke();
        }

        public void StopAnimation() => animator.speed = 0;
        public void PlayAnimation() => animator.speed = 1;
        
        #region Animator Parameter Section

        public void SetParam(AnimParamSO param, bool value) => animator.SetBool(param.paramHash, value);
        public void SetParam(AnimParamSO param, float value) => animator.SetFloat(param.paramHash, value);
        public void SetParam(AnimParamSO param, int value) => animator.SetInteger(param.paramHash, value);
        public void SetParam(AnimParamSO param) => animator.SetTrigger(param.paramHash);

        #endregion

        #region Character Flip Controller Section

        public void FlipController(float xMove)
        {
            if (Mathf.Abs(FacingDirection + xMove) < 0.5f)
            {
                Flip();
            }
        }
    
        public void Flip()
        {
            FacingDirection *= -1;

            Vector3 localScale = _entity.Transform.localScale;
            localScale.x = Mathf.Abs(localScale.x) * (FacingDirection > 0 ? 1f : -1f);
            _entity.Transform.localScale = localScale;
        }
        
        public void FlipTowardsTarget(Transform target)
        {
            float targetX = target.position.x;
            float selfX = _entity.Transform.position.x;

            float xDir = targetX - selfX;

            FlipController(Mathf.Sign(xDir));
        }

        #endregion
        
    }
}