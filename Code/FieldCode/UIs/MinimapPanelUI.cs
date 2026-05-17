using DG.Tweening;
using PSW.Code.BaseSystem;
using UnityEngine;

namespace Work.PSB.Code.FieldCode.UIs
{
    public class MinimapPanelUI : BaseOnOffSystemUI 
    {
        [Header("Minimap UI Components")]
        [SerializeField] private RectTransform minimapRect;
        [SerializeField] private CanvasGroup darkBackground;
        [SerializeField] private RectTransform centerContainer;

        [Header("Animation Settings")]
        [SerializeField] private float animDuration = 0.35f;
        [SerializeField] private Vector2 expandSize = new Vector2(800f, 800f);

        private Transform _originalParent;
        private Vector2 _originalPosition;
        private Vector2 _originalSize;
        private bool _isTransitioning;

        private void Awake()
        {
            if (darkBackground != null)
            {
                darkBackground.alpha = 0f;
                darkBackground.gameObject.SetActive(false);
            }

            if (minimapRect != null)
            {
                _originalParent = minimapRect.parent;
                _originalPosition = minimapRect.anchoredPosition;
                _originalSize = minimapRect.sizeDelta;
            }
        }

        public override void PopUp()
        {
            if (IsOnPopUp() == false) return; 

            base.PopUp();

            if (_isTransitioning) return;
            _isTransitioning = true;
            
            minimapRect.DOKill();
            darkBackground.DOKill();

            if (darkBackground != null)
            {
                darkBackground.gameObject.SetActive(true);
                darkBackground.DOFade(1f, animDuration).SetUpdate(true);
            }

            if (minimapRect != null && centerContainer != null)
            {
                minimapRect.SetParent(centerContainer, true); 
                minimapRect.DOMove(centerContainer.position, animDuration).SetUpdate(true).SetEase(Ease.OutCubic);
                minimapRect.DOSizeDelta(expandSize, animDuration).SetUpdate(true).SetEase(Ease.OutCubic)
                    .OnComplete(() => _isTransitioning = false);
            }
        }

        public override void PopDown()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            minimapRect.DOKill();
            darkBackground.DOKill();

            if (darkBackground != null)
            {
                darkBackground.DOFade(0f, animDuration).SetUpdate(true);
            }

            if (minimapRect != null)
            {
                minimapRect.SetParent(_originalParent, true);
                minimapRect.DOAnchorPos(_originalPosition, animDuration).SetUpdate(true).SetEase(Ease.OutCubic);
                minimapRect.DOSizeDelta(_originalSize, animDuration).SetUpdate(true).SetEase(Ease.OutCubic)
                    .OnComplete(() =>
                    {
                        if (darkBackground != null) darkBackground.gameObject.SetActive(false);
                        _isTransitioning = false;
                        
                        base.PopDown(); 
                    });
            }
            else
            {
                _isTransitioning = false;
                base.PopDown();
            }
        }
        
    }
}