using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    public class TutorialArchiveListItemUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private GameObject selectedObject;

        private TutorialDataSO _data;
        private Action<TutorialDataSO> _onClick;

        public TutorialDataSO Data => _data;

        public void Bind(TutorialDataSO data, Action<TutorialDataSO> onClick)
        {
            _data = data;
            _onClick = onClick;

            if (titleText != null)
                titleText.text = data != null ? data.DisplayName : "-";

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectedObject != null)
                selectedObject.SetActive(selected);
        }

        private void HandleClick()
        {
            _onClick?.Invoke(_data);
        }
        
        public void SetInteractable(bool v)
        {
            if (button != null)
                button.interactable = v;
        }
        
    }
}