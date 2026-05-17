using DG.Tweening;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using Work.PSB.Code.CoreSystem;

namespace PSB.Code.BattleCode.UIs
{
    public class AlarmTutoUI : MonoBehaviour
    {
        [SerializeField] private GameObject tutoPanel;
        [SerializeField] private TransitionController transition;
        [SerializeField] protected float tweenDuration = 0.25f;
        
        private CanvasGroup _canvasGroup;
        private RectTransform _rect;
        private Tween _tween;

        private void Awake()
        {
            _rect = transform as RectTransform;
            _canvasGroup = GetComponent<CanvasGroup>();
            SetImmediate(false);
        }

        private void OnEnable()
        {
            Bus<FirstTutoEvent>.OnEvent += HandleFirstTutoEvent;
        }

        private void OnDisable()
        {
            Bus<FirstTutoEvent>.OnEvent -= HandleFirstTutoEvent;
        }

        private void HandleFirstTutoEvent(FirstTutoEvent evt)
        {
            PopUp();
        }

        public void TutoBtnClick()
        {
            transition.nextScene = "PSB_Tuto_Field";
            transition.Transition();
            PopDown();
        }
        
        private void PopUp()
        {
            _tween?.Kill();

            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            _rect.anchoredPosition = new Vector2(0f, -50f);
            _canvasGroup.alpha = 0f;

            _tween = DOTween.Sequence()
                .Append(_rect.DOAnchorPosY(0f, tweenDuration).SetEase(Ease.OutCubic))
                .Join(_canvasGroup.DOFade(1f, tweenDuration));
        }

        public void PopDown()
        {
            _tween?.Kill();

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _tween = DOTween.Sequence()
                .Append(_rect.DOAnchorPosY(-50f, tweenDuration).SetEase(Ease.InCubic))
                .Join(_canvasGroup.DOFade(0f, tweenDuration))
                .OnComplete(() => _canvasGroup.alpha = 0);
        }
        
        private void SetImmediate(bool open)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = open ? 1f : 0f;
                _canvasGroup.interactable = open;
                _canvasGroup.blocksRaycasts = open;
            }

            if (_rect != null)
            {
                _rect.anchoredPosition = open
                    ? Vector2.zero
                    : new Vector2(0f, -50f);
            }
        }
        
    }
}