using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.PSB.Code.RunSystem
{
    public class RunEventSelectionItemUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private GameObject selectedMark;
        
        private RunEventSelectionCandidate _candidate;
        private Action<RunEventSelectionCandidate> _onClicked;
        
        public void Bind(RunEventSelectionCandidate candidate, Action<RunEventSelectionCandidate> onClicked)
        {
            _candidate = candidate;
            _onClicked = onClicked;
            
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
                button.onClick.AddListener(HandleClicked);
            }
            
            if (iconImage != null)
            {
                iconImage.sprite = candidate != null ? candidate.Icon : null;
                iconImage.enabled = iconImage.sprite != null;
            }
            
            if (nameText != null)
                nameText.text = candidate != null ? candidate.DisplayName : string.Empty;
            
            if (descriptionText != null)
                descriptionText.text = candidate != null ? candidate.Description : string.Empty;
            
            SetSelected(false);
        }
        
        public void SetSelected(bool selected)
        {
            if (selectedMark != null)
                selectedMark.SetActive(selected);
        }
        
        private void HandleClicked()
        {
            if (_candidate != null)
                _onClicked?.Invoke(_candidate);
        }
        
        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClicked);
        }
        
    }
}
