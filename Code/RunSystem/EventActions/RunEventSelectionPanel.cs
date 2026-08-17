using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work.PSB.Code.RunSystem
{
    public class RunEventSelectionPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Transform content;
        [SerializeField] private RunEventSelectionItemUI itemPrefab;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        
        private readonly List<RunEventSelectionCandidate> _selected = new List<RunEventSelectionCandidate>();
        private readonly Dictionary<string, RunEventSelectionItemUI> _items =
            new Dictionary<string, RunEventSelectionItemUI>();
        
        private RunEventSelectionSettings _settings;
        private Func<IReadOnlyList<RunEventSelectionCandidate>, bool> _selectionValidator;
        private Action<IReadOnlyList<RunEventSelectionCandidate>> _onConfirmed;
        private Action _onCancelled;
        
        public bool IsConfigured => content != null && itemPrefab != null &&
                                    confirmButton != null && cancelButton != null;
        
        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(Confirm);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(Cancel);
            
            SetVisible(false);
        }
        
        public void Open(RunEventSelectionSettings settings,
            IReadOnlyList<RunEventSelectionCandidate> candidates,
            Func<IReadOnlyList<RunEventSelectionCandidate>, bool> selectionValidator,
            Action<IReadOnlyList<RunEventSelectionCandidate>> onConfirmed, Action onCancelled)
        {
            ClearItems();
            
            _settings = settings;
            _selectionValidator = selectionValidator;
            _onConfirmed = onConfirmed;
            _onCancelled = onCancelled;
            
            if (titleText != null)
                titleText.text = settings != null ? settings.title : string.Empty;
            
            if (candidates != null)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    RunEventSelectionCandidate candidate = candidates[i];
                    RunEventSelectionItemUI item = Instantiate(itemPrefab, content);
                    item.Bind(candidate, ToggleSelection);
                    _items[candidate.CandidateId] = item;
                }
            }
            
            SetVisible(true);
            RefreshState();
        }
        
        public void Cancel()
        {
            Action callback = _onCancelled;
            Close();
            callback?.Invoke();
        }
        
        private void ToggleSelection(RunEventSelectionCandidate candidate)
        {
            if (candidate == null || _settings == null)
                return;
            
            int selectedIndex = _selected.FindIndex(item => item.CandidateId == candidate.CandidateId);
            if (selectedIndex >= 0)
            {
                _selected.RemoveAt(selectedIndex);
            }
            else
            {
                if (_selected.Count >= _settings.MaximumCount)
                    return;
                
                _selected.Add(candidate);
            }
            
            RefreshState();
        }
        
        private void RefreshState()
        {
            foreach (KeyValuePair<string, RunEventSelectionItemUI> pair in _items)
            {
                bool selected = _selected.Exists(item => item.CandidateId == pair.Key);
                pair.Value.SetSelected(selected);
            }
            
            int minimum = _settings != null ? _settings.MinimumCount : 0;
            int maximum = _settings != null ? _settings.MaximumCount : 0;
            
            if (countText != null)
                countText.text = $"{_selected.Count} / {minimum}-{maximum}";
            
            bool countValid = _selected.Count >= minimum && _selected.Count <= maximum;
            bool customValid = countValid && (_selectionValidator == null || _selectionValidator.Invoke(_selected));
            
            if (confirmButton != null)
                confirmButton.interactable = customValid;
        }
        
        private void Confirm()
        {
            if (_settings == null)
                return;
            
            bool valid = _selected.Count >= _settings.MinimumCount &&
                         _selected.Count <= _settings.MaximumCount &&
                         (_selectionValidator == null || _selectionValidator.Invoke(_selected));
            if (!valid)
                return;
            
            List<RunEventSelectionCandidate> result = new List<RunEventSelectionCandidate>(_selected);
            Action<IReadOnlyList<RunEventSelectionCandidate>> callback = _onConfirmed;
            Close();
            callback?.Invoke(result);
        }
        
        private void Close()
        {
            SetVisible(false);
            ClearItems();
            _settings = null;
            _selectionValidator = null;
            _onConfirmed = null;
            _onCancelled = null;
        }
        
        private void ClearItems()
        {
            foreach (RunEventSelectionItemUI item in _items.Values)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            
            _items.Clear();
            _selected.Clear();
        }
        
        private void SetVisible(bool visible)
        {
            GameObject target = panelRoot != null ? panelRoot : gameObject;
            target.SetActive(visible);
        }
        
        private void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(Confirm);
            
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(Cancel);
        }
        
    }
}
