using System.Collections;
using DG.Tweening;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public class EnemyHitReaction : MonoBehaviour, IModule
    {
        [Header("Hit Motion")]
        [SerializeField] private float hitPushDistance = 0.25f;
        [SerializeField] private float hitPushTime = 0.08f;
        [SerializeField] private float hitReturnTime = 0.12f;
        [SerializeField] private float hitHoldTime = 0.12f;

        [Header("Hit Timing")]
        [SerializeField] private float hitAnimTotalTime = 0.35f;
        
        private Coroutine _hitCo;

        private object _tweenId;

        public void Initialize(ModuleOwner owner)
        {
            _tweenId = this;
        }
        
        /*public void InitializeAnim()
        {
            _tweenId = this;
        }*/
        
        public void PlayHit()
        {
            if (_hitCo != null) StopCoroutine(_hitCo);
            _hitCo = StartCoroutine(HitCoroutine());
        }

        private IEnumerator HitCoroutine()
        {
            PlayHitFX();

            yield return new WaitForSeconds(hitAnimTotalTime);

            _hitCo = null;
        }

        private void PlayHitFX()
        {
            Vector3 dir = Vector3.right;

            Vector3 startPos = transform.position;
            Vector3 pushedPos = startPos + dir.normalized * hitPushDistance;

            DOTween.Kill(_tweenId);

            Sequence seq = DOTween.Sequence().SetId(_tweenId);
            seq.Append(transform.DOMove(pushedPos, hitPushTime).SetEase(Ease.OutQuad));
            seq.AppendInterval(hitHoldTime);
            seq.Append(transform.DOMove(startPos, hitReturnTime).SetEase(Ease.OutQuad));
        }

        private void OnDisable()
        {
            if (_hitCo != null)
            {
                StopCoroutine(_hitCo);
                _hitCo = null;
            }

            if (_tweenId != null)
                DOTween.Kill(_tweenId);
        }
        
    }
}
