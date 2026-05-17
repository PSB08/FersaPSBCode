using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    public class TutorialArchiveSearchView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button clearSearchButton;

        public event Action<string> OnSearchValueChanged;

        public string Keyword
        {
            get
            {
                if (searchInput == null || string.IsNullOrWhiteSpace(searchInput.text))
                    return string.Empty;

                return searchInput.text.Trim();
            }
        }

        public bool IsFocused => searchInput != null && searchInput.isFocused;

        private void OnEnable()
        {
            if (searchInput != null)
                searchInput.onValueChanged.AddListener(HandleSearchChanged);

            if (clearSearchButton != null)
                clearSearchButton.onClick.AddListener(OnClickClearSearch);

            RefreshClearButton();
        }

        private void OnDisable()
        {
            if (searchInput != null)
                searchInput.onValueChanged.RemoveListener(HandleSearchChanged);

            if (clearSearchButton != null)
                clearSearchButton.onClick.RemoveListener(OnClickClearSearch);
        }

        public void SetInteractable(bool value)
        {
            if (searchInput != null)
                searchInput.interactable = value;

            if (clearSearchButton != null)
                clearSearchButton.interactable = value;
        }

        private void HandleSearchChanged(string value)
        {
            RefreshClearButton();
            OnSearchValueChanged?.Invoke(value);
        }

        private void RefreshClearButton()
        {
            if (clearSearchButton == null || searchInput == null)
                return;

            bool hasText = !string.IsNullOrWhiteSpace(searchInput.text);
            clearSearchButton.gameObject.SetActive(hasText);
        }

        private void OnClickClearSearch()
        {
            if (searchInput == null)
                return;

            searchInput.text = string.Empty;
            searchInput.caretPosition = 0;
            searchInput.stringPosition = 0;
            searchInput.selectionAnchorPosition = 0;
            searchInput.selectionFocusPosition = 0;
            searchInput.ForceLabelUpdate();

            RefreshClearButton();
            OnSearchValueChanged?.Invoke(string.Empty);

            searchInput.DeactivateInputField();
        }
        
    }
}