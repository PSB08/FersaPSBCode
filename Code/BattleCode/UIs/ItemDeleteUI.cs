using PSW.Code.EventBus;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Work.PSB.Code.CoreSystem.Sounds;
using YIS.Code.Items;

namespace PSB.Code.BattleCode.UIs
{
    public class ItemDeleteUI : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;

        [Header("Count Input")]
        [SerializeField] private TMP_InputField countInput;

        [Header("Buttons")]
        [SerializeField] private Button minusButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("Sound")]
        [SerializeField] private SoundSO uiClickSound;
        [SerializeField] private SoundSO uiErrorSound;
        
        public bool IsInputEditing =>
            gameObject.activeInHierarchy &&
            countInput != null &&
            countInput.isFocused;

        private int _slotIndex;
        private int _max;
        private int _count;

        private Action<int, int> _onConfirm;

        private bool _suppressCommit;

        private void Awake()
        {
            if (minusButton != null)
                minusButton.onClick.AddListener(OnMinus);

            if (plusButton != null)
                plusButton.onClick.AddListener(OnPlus);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirm);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancel);

            SetupCountInput();
            HideImmediate();
        }

        private void SetupCountInput()
        {
            if (countInput == null) return;
            
            countInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            countInput.characterValidation = TMP_InputField.CharacterValidation.Integer;
            countInput.onValidateInput += (string text, int charIndex, char addedChar) =>
                char.IsDigit(addedChar) ? addedChar : '\0';
            
            countInput.onEndEdit.AddListener(OnInputCommitted);
            countInput.onSubmit.AddListener(OnInputCommitted);
            
            countInput.onDeselect.AddListener(_ => OnInputCommitted(countInput.text));
        }

        public void Show(ItemVisualDataSO visual, int slotIndex, int maxCount, Action<int, int> onConfirm)
        {
            _slotIndex = slotIndex;
            _max = Mathf.Max(1, maxCount);
            _count = 1;
            _onConfirm = onConfirm;

            if (iconImage != null)
            {
                iconImage.enabled = (visual != null && visual.icon != null);
                iconImage.sprite = visual != null ? visual.icon : null;
            }

            if (nameText != null)
                nameText.SetText(visual != null ? visual.uiName : "-");

            SetInputText(_count);
            RefreshButtons();

            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
        }

        private void OnInputCommitted(string raw)
        {
            if (_suppressCommit) return;
            
            if (_max <= 0) return;

            int parsed;
            if (string.IsNullOrEmpty(raw))
            {
                parsed = 1;
            }
            else if (!int.TryParse(raw, out parsed))
            {
                parsed = 1;
            }

            parsed = Mathf.Clamp(parsed, 1, _max);
            _count = parsed;
            
            SetInputText(_count);
            
            RefreshButtons();
        }

        private void SetInputText(int value)
        {
            if (countInput == null) return;

            _suppressCommit = true;
            countInput.SetTextWithoutNotify(value.ToString());
            _suppressCommit = false;
        }

        private void RefreshButtons()
        {
            _count = Mathf.Clamp(_count, 1, _max);

            if (minusButton != null)
                minusButton.interactable = (_count > 1);

            if (plusButton != null)
                plusButton.interactable = (_count < _max);

            if (confirmButton != null)
                confirmButton.interactable = (_max > 0);
        }

        private void OnMinus()
        {
            OnInputCommitted(countInput != null ? countInput.text : _count.ToString());

            if (_count <= 1)
            {
                PlaySfx(transform.position, uiErrorSound);
                RefreshButtons();
                return;
            }

            PlaySfx(transform.position, uiClickSound);

            _count = Mathf.Clamp(_count - 1, 1, _max);
            SetInputText(_count);
            RefreshButtons();
        }

        private void OnPlus()
        {
            OnInputCommitted(countInput != null ? countInput.text : _count.ToString());

            if (_count >= _max)
            {
                PlaySfx(transform.position, uiErrorSound);
                RefreshButtons();
                return;
            }

            PlaySfx(transform.position, uiClickSound);

            _count = Mathf.Clamp(_count + 1, 1, _max);
            SetInputText(_count);
            RefreshButtons();
        }

        private void OnConfirm()
        {
            OnInputCommitted(countInput != null ? countInput.text : _count.ToString());

            if (_onConfirm == null)
            {
                PlaySfx(transform.position, uiErrorSound);
                return;
            }

            PlaySfx(transform.position, uiClickSound);

            _onConfirm?.Invoke(_slotIndex, _count);
            Hide();
        }

        private void OnCancel()
        {
            PlaySfx(transform.position, uiClickSound);
            Hide();
        }

        public void Hide()
        {
            _onConfirm = null;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            gameObject.SetActive(false);
        }

        private void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            gameObject.SetActive(false);
        }

        private void PlaySfx(Vector3 position, SoundSO soundSO)
        {
            if (soundSO == null || soundSO.clip == null) return;
            
            Bus<PlaySFXEvent>.Raise(SoundEvents.PlaySFXEvent.Initialize(position, soundSO));
        }
        
    }
}