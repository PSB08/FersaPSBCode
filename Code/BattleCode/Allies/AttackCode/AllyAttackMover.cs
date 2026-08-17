using DG.Tweening;
using UnityEngine;

namespace PSB.Code.BattleCode.Allies.AttackCode
{
    public sealed class AllyAttackMover
    {
        private readonly Transform _transform;
        private readonly float _dashDuration;
        private readonly Ease _dashEase;
        private readonly object _dashTweenId;

        public AllyAttackMover(Transform transform, float dashDuration, Ease dashEase)
        {
            _transform = transform;
            _dashDuration = dashDuration;
            _dashEase = dashEase;
            _dashTweenId = (transform, "ALLY_DASH");
        }

        public Vector3 Position => _transform.position;

        public void Kill()
        {
            if (DOTween.instance == null)
                return;

            DOTween.Kill(_dashTweenId);
        }

        public Vector3 GetForwardDashPos(Vector3 startPos, Vector3 targetPos, float distance)
        {
            float direction = targetPos.x >= startPos.x ? 1f : -1f;

            return new Vector3(
                startPos.x + direction * Mathf.Abs(distance),
                startPos.y,
                startPos.z
            );
        }

        public Tween AnticipateAndDashTo(Vector3 dashPos, float backDistance = 0.1f,
            float backTime = 0.07f, float dashTime = 0.04f)
        {
            Vector3 start = _transform.position;
            Vector3 dir = dashPos - start;

            if (dir.sqrMagnitude <= 0.0001f)
                dir = Vector3.right;

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
