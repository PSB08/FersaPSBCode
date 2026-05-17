using System.Collections;
using CIW.Code.Entities;
using DG.Tweening;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Players
{
    public class PlayerHitReaction : MonoBehaviour, IModule
    {
        [Header("Hit")]
        [SerializeField] private float hitPushDistance = 0.25f;
        [SerializeField] private float hitPushTime = 0.08f;
        [SerializeField] private float hitReturnTime = 0.12f;
        [SerializeField] private float hitHoldTime = 0.12f;

        private EntityAnimator _animator;
        private Coroutine _hitCo;

        public void Initialize(ModuleOwner owner)
        {
            _animator = owner != null ? owner.GetModule<EntityAnimator>() : null;
        }

        public void PlayHit()
        {
            if (_hitCo != null) StopCoroutine(_hitCo);
            _hitCo = StartCoroutine(CoHit());
        }

        private IEnumerator CoHit()
        {
            int idleHash = Animator.StringToHash("IDLE");
            int hitHash  = Animator.StringToHash("HIT");
            
            if (_animator != null)
            {
                _animator.PlayClip(hitHash);
            }

            PlayHitFX();
            yield return new WaitForSeconds(0.45f);

            if (_animator != null)
            {
                _animator.PlayClip(idleHash);
            }

            _hitCo = null;
        }

        private void PlayHitFX()
        {
            Vector3 dir = Vector3.right;
            Vector3 startPos  = transform.position;
            Vector3 pushedPos = startPos + dir * hitPushDistance;

            DOTween.Kill("HitFX");
            Sequence seq = DOTween.Sequence().SetId("HitFX");
            seq.Append(transform.DOMove(pushedPos, hitPushTime).SetEase(Ease.OutQuad));
            seq.AppendInterval(hitHoldTime);
            seq.Append(transform.DOMove(startPos, hitReturnTime).SetEase(Ease.OutQuad));
        }
        
    }
}