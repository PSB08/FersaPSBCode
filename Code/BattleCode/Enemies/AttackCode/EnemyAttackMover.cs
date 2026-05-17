using DG.Tweening;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public class EnemyAttackMover
    {
        private readonly Transform _transform;
        private readonly float _dashDuration;
        private readonly Ease _dashEase;

        private readonly object _dashTweenId;

        public EnemyAttackMover(Transform transform, float dashDuration, Ease dashEase)
        {
            _transform = transform;
            _dashDuration = dashDuration;
            _dashEase = dashEase;

            _dashTweenId = (transform, "DASH");
        }

        public Vector3 Position => _transform.position;

        public void Kill()
        {
            DOTween.Kill(_dashTweenId);
        }

        public Vector3 GetDashTargetPos(Vector3 playerPos)
        {
            Vector3 startPos = _transform.position;

            return new Vector3(
                playerPos.x,
                playerPos.y,
                startPos.z
            );
        }

        public Tween AnticipateAndDashTo(Vector3 dashPos, float backDistance = 0.1f, 
            float backTime = 0.07f, float dashTime = 0.04f)
        {
            Vector3 start = _transform.position;

            Vector3 dir = dashPos - start;
            dir.Normalize();
            
            Vector3 backPos = start - dir * backDistance;
            backPos.z = start.z;

            Sequence seq = DOTween.Sequence()
                .SetId(_dashTweenId)
                .Append(_transform.DOMove(backPos, backTime).SetEase(Ease.OutQuad))
                .Append(_transform.DOMove(dashPos, dashTime).SetEase(Ease.InQuad))  
                .SetUpdate(UpdateType.Normal, true);

            return seq;
        }

        public Tween ReturnTo(Vector3 startPos)
        {
            return _transform
                .DOMove(startPos, _dashDuration * 0.7f)
                .SetEase(_dashEase)
                .SetId(_dashTweenId);
        }
        
    }
}