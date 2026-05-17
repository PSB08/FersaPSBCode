using DG.Tweening;
using PSB_Lib.Dependencies;
using PSB.Code.BattleCode.Players;
using UnityEngine;

namespace PSB.Code.BattleCode.Items
{
    public class ItemAutoDestroy : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float lifeTime = 1f;

        [Header("Fly To Player")]
        [SerializeField] private float flyDuration = 0.18f;
        [SerializeField] private Ease flyEase = Ease.InQuad;

        [Header("Shrink")]
        [SerializeField] private float shrinkDuration = 0.18f;
        [SerializeField] private Ease shrinkEase = Ease.InBack;

        [Header("Offset (optional)")]
        [SerializeField] private Vector3 targetOffset = Vector3.zero;

        private PlayerManager _playerManager;

        private Sequence _seq;
        private bool _isConsuming;

        private void Awake()
        {
            _playerManager = FindFirstObjectByType<PlayerManager>();
        }
        
        private void Start()
        {
            Invoke(nameof(ConsumeToPlayer), lifeTime);
        }

        private void ConsumeToPlayer()
        {
            if (_isConsuming) return;
            _isConsuming = true;

            var player = _playerManager != null ? _playerManager.BattlePlayer : null;
            if (player == null)
            {
                ShrinkAndDestroy();
                return;
            }

            var targetPos = player.transform.position + targetOffset;

            _seq?.Kill(false);
            _seq = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Join(transform.DOMove(targetPos, flyDuration).SetEase(flyEase))
                .Join(transform.DOScale(Vector3.zero, shrinkDuration).SetEase(shrinkEase))
                .OnComplete(() => Destroy(gameObject));
        }

        private void ShrinkAndDestroy()
        {
            _seq?.Kill(false);
            _seq = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Append(transform.DOScale(Vector3.zero, shrinkDuration).SetEase(shrinkEase))
                .OnComplete(() => Destroy(gameObject));
        }

        private void OnDestroy()
        {
            if (_seq != null && _seq.IsActive())
                _seq.Kill(false);
        }
        
    }
}