using DG.Tweening;
using PSB.Code.BattleCode.UIs;
using UnityEngine;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    public class TutorialWindowAnimator : MonoBehaviour
    {
        [Header("Size Animation")]
        [SerializeField] private EndPanelSizeToggle_Model sizeModel;
        [SerializeField] private EndPanelSizeToggle_View sizeView;

        [Header("White Window")]
        [SerializeField] private CanvasGroup whiteWindow;
        [SerializeField] private float whiteFadeTime = 0.12f;

        private Sequence _seq;
        private bool _isShown;

        public bool IsShown => _isShown;

        public void Init()
        {
            sizeView.Init(sizeModel.InitModel());

            if (whiteWindow != null)
            {
                whiteWindow.alpha = 0f;
                whiteWindow.interactable = false;
                whiteWindow.blocksRaycasts = false;
            }

            _isShown = false;
        }

        public void PlayShow(System.Action<bool> setButtonsInteractable)
        {
            if (_isShown) return;
            _isShown = true;

            _seq?.Kill();
            _seq = DOTween.Sequence().SetUpdate(true);

            _seq.AppendCallback(() =>
            {
                if (whiteWindow != null)
                {
                    whiteWindow.gameObject.SetActive(true);
                    whiteWindow.interactable = false;
                    whiteWindow.blocksRaycasts = false;
                }
                setButtonsInteractable?.Invoke(false);
            });
            
            if (whiteWindow != null)
                _seq.Append(whiteWindow.DOFade(1f, whiteFadeTime));
            
            _seq.AppendCallback(() =>
            {
                sizeView.AnimateToHeight(
                    sizeModel.GetShowSize(),
                    sizeModel.GetPopTime()
                );
            });

            _seq.AppendInterval(sizeModel.GetPopTime());

            _seq.AppendCallback(() =>
            {
                if (whiteWindow != null)
                {
                    whiteWindow.interactable = true;
                    whiteWindow.blocksRaycasts = true;
                }
                setButtonsInteractable?.Invoke(true);
            });
        }

        public void PlayHide(System.Action<bool> setButtonsInteractable, System.Action onComplete)
        {
            if (!_isShown) return;
            _isShown = false;

            _seq?.Kill();
            _seq = DOTween.Sequence().SetUpdate(true);

            _seq.AppendCallback(() =>
            {
                setButtonsInteractable?.Invoke(false);

                if (whiteWindow != null)
                {
                    whiteWindow.interactable = false;
                    whiteWindow.blocksRaycasts = false;
                }
            });
            
            _seq.AppendCallback(() =>
            {
                sizeView.AnimateToHeight(
                    sizeModel.GetHideSize(),
                    sizeModel.GetPopTime()
                );
            });

            _seq.AppendInterval(sizeModel.GetPopTime());
            
            if (whiteWindow != null)
                _seq.Append(whiteWindow.DOFade(0f, whiteFadeTime));

            _seq.AppendCallback(() => onComplete?.Invoke());
        }
    
    }
}