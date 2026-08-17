using PSW.Code.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.PSB.Code.CoreSystem.Sounds;

namespace PSB.Code.BattleCode.UIs
{
    public class SystemLogTooltipUI : MonoBehaviour
    {
        public static SystemLogTooltipUI Instance { get; private set; }

        [SerializeField] private RectTransform panelRect;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Scroll")]
        [SerializeField] private ScrollRect descriptionScrollRect;
        [SerializeField] private RectTransform descriptionContent;

        [SerializeField] private SoundSO uiClickSound;

        private string _currentLinkId;
        private bool _isShowing;

        private void Awake()
        {
            Instance = this;
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Toggle(string linkId, string title, string description, Vector2 screenPosition)
        {
            if (_isShowing && _currentLinkId == linkId)
            {
                Hide();
                return;
            }

            Show(linkId, title, description, screenPosition);
            Bus<PlaySFXEvent>.Raise(SoundEvents.PlaySFXEvent.Initialize(transform.position, uiClickSound));
        }

        public void Show(string linkId, string title, string description, Vector2 screenPosition)
        {
            if (panelRect == null || canvasGroup == null) return;

            _currentLinkId = linkId;
            _isShowing = true;

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (descriptionText != null)
            {
                descriptionText.overflowMode = TextOverflowModes.Overflow;
                descriptionText.text = description;
            }

            panelRect.gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            RefreshScrollLayout();
        }

        public void Hide()
        {
            _currentLinkId = null;
            _isShowing = false;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (panelRect != null)
            {
                panelRect.gameObject.SetActive(false);
            }
        }

        private void RefreshScrollLayout()
        {
            if (descriptionText != null)
            {
                descriptionText.ForceMeshUpdate();
            }

            if (descriptionContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionContent);
            }

            if (panelRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            }

            Canvas.ForceUpdateCanvases();
        }
        
    }
}