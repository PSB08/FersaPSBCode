using CIW.Code.Player.Field;
using DG.Tweening;
using PSW.Code.BaseSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    public class TutorialArchivePanelUI : MonoBehaviour, IBaseSystemUI
    {
        [Header("Database")]
        [SerializeField] private TutorialArchiveSO database;

        [Header("Views")]
        [SerializeField] private TutorialArchiveSearchView searchView;
        [SerializeField] private TutorialArchiveListView listView;
        [SerializeField] private TutorialArchiveDetailView detailView;

        [Header("Input")]
        [SerializeField] private PlayerFieldInputSO fieldInputSO;

        private readonly List<TutorialDataSO> _filtered = new();

        private int _selectedTutorialIndex = -1;
        private int _currentPageIndex;
        private bool _isTransitioning;

        private TutorialDataSO CurrentTutorial
        {
            get
            {
                if (_selectedTutorialIndex < 0 || _selectedTutorialIndex >= _filtered.Count)
                    return null;

                return _filtered[_selectedTutorialIndex];
            }
        }
        private void OnEnable()
        {
            TutorialArchiveSave.OnViewedChanged += OnViewedChanged;

            if (searchView != null)
                searchView.OnSearchValueChanged += OnSearchChanged;

            if (listView != null)
                listView.OnItemClicked += OnClickItem;

            if (detailView != null)
            {
                detailView.OnPrevClicked += OnPrev;
                detailView.OnNextClicked += OnNext;
            }

            RefreshAll();
        }

        private void OnDisable()
        {
            TutorialArchiveSave.OnViewedChanged -= OnViewedChanged;

            if (searchView != null)
                searchView.OnSearchValueChanged -= OnSearchChanged;

            if (listView != null)
                listView.OnItemClicked -= OnClickItem;

            if (detailView != null)
            {
                detailView.OnPrevClicked -= OnPrev;
                detailView.OnNextClicked -= OnNext;
            }

            SetFieldInputEnabled(true);
        }

        public bool CanToggle()
        {
            return !_isTransitioning && (searchView == null || !searchView.IsFocused);
        }


        private void SetFieldInputEnabled(bool enabled)
        {
            if (fieldInputSO == null)
                return;

            if (enabled) fieldInputSO.EnableInput();
            else fieldInputSO.DisableInput();
        }

        private void SetViewsInteractable(bool value)
        {
            searchView?.SetInteractable(value);
            listView?.SetInteractable(value);
            detailView?.SetInteractable(value);

            if (value)
                RefreshNavigationState();
        }

        private void OnViewedChanged(string tutorialId, bool viewed)
        {
            RefreshAll();
        }

        private void OnSearchChanged(string _)
        {
            RefreshAll();
        }

        public void RefreshAll()
        {
            RefreshFilteredTutorials();
            ValidateSelection();

            listView?.Bind(_filtered, _selectedTutorialIndex);
            RefreshDetailView();
        }

        private void RefreshFilteredTutorials()
        {
            _filtered.Clear();

            if (database == null || database.Tutorials == null)
                return;

            string keyword = searchView != null ? searchView.Keyword : string.Empty;

            foreach (var tutorial in database.Tutorials)
            {
                if (IsMatchedTutorial(tutorial, keyword))
                    _filtered.Add(tutorial);
            }
        }

        private bool IsMatchedTutorial(TutorialDataSO tutorial, string keyword)
        {
            if (tutorial == null)
                return false;

            if (!TutorialArchiveSave.IsViewed(tutorial.TutorialId))
                return false;

            if (string.IsNullOrEmpty(keyword))
                return true;

            if (string.IsNullOrEmpty(tutorial.DisplayName))
                return false;

            return tutorial.DisplayName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ValidateSelection()
        {
            if (_filtered.Count == 0)
            {
                _selectedTutorialIndex = -1;
                _currentPageIndex = 0;
                return;
            }

            if (_selectedTutorialIndex < 0 || _selectedTutorialIndex >= _filtered.Count)
            {
                _selectedTutorialIndex = 0;
                _currentPageIndex = 0;
            }

            var tutorial = CurrentTutorial;
            if (tutorial == null || tutorial.PageCount <= 0)
            {
                _currentPageIndex = 0;
                return;
            }

            _currentPageIndex = Mathf.Clamp(_currentPageIndex, 0, tutorial.PageCount - 1);
        }

        private void RefreshDetailView()
        {
            bool hasAny = _filtered.Count > 0;
            detailView?.SetEmptyState(!hasAny);

            var tutorial = CurrentTutorial;
            if (tutorial == null || tutorial.PageCount <= 0)
            {
                detailView?.Clear();
                RefreshNavigationState();
                return;
            }

            detailView?.ShowPage(tutorial, _currentPageIndex);
            RefreshNavigationState();
        }

        private void RefreshNavigationState()
        {
            detailView?.SetNavigation(CanGoPrev(), CanGoNext());
        }

        private bool CanGoPrev()
        {
            var tutorial = CurrentTutorial;
            if (tutorial == null)
                return false;

            if (_currentPageIndex > 0)
                return true;

            return _selectedTutorialIndex > 0;
        }

        private bool CanGoNext()
        {
            var tutorial = CurrentTutorial;
            if (tutorial == null)
                return false;

            if (_currentPageIndex < tutorial.PageCount - 1)
                return true;

            return _selectedTutorialIndex < _filtered.Count - 1;
        }

        private void OnClickItem(int tutorialIndex)
        {
            if (_isTransitioning || tutorialIndex < 0 || tutorialIndex >= _filtered.Count)
                return;

            _selectedTutorialIndex = tutorialIndex;
            _currentPageIndex = 0;

            listView?.SetSelected(_selectedTutorialIndex);
            RefreshDetailView();
        }

        public void OnPrev()
        {
            if (_isTransitioning) return;

            var tutorial = CurrentTutorial;
            if (tutorial == null)
                return;

            if (_currentPageIndex > 0)
            {
                _currentPageIndex--;
                RefreshDetailView();
                return;
            }

            if (_selectedTutorialIndex > 0)
            {
                _selectedTutorialIndex--;
                var prevTutorial = CurrentTutorial;
                _currentPageIndex = Mathf.Max(0, prevTutorial.PageCount - 1);

                listView?.SetSelected(_selectedTutorialIndex);
                RefreshDetailView();
            }
        }

        public void OnNext()
        {
            if (_isTransitioning) return;

            var tutorial = CurrentTutorial;
            if (tutorial == null)
                return;

            if (_currentPageIndex < tutorial.PageCount - 1)
            {
                _currentPageIndex++;
                RefreshDetailView();
                return;
            }

            if (_selectedTutorialIndex < _filtered.Count - 1)
            {
                _selectedTutorialIndex++;
                _currentPageIndex = 0;

                listView?.SetSelected(_selectedTutorialIndex);
                RefreshDetailView();
            }
        }

        [ContextMenu("Reset All Tutorial Archive")]
        public void ResetAllArchive()
        {
            if (database == null)
                return;

            TutorialArchiveSave.ResetAll(database.Tutorials);
            _selectedTutorialIndex = -1;
            _currentPageIndex = 0;
            RefreshAll();

            Debug.Log("[TutorialArchive] 전체 리셋 완료", this);
        }

        public void DataInit()
        {
            _isTransitioning = true;

            RefreshAll();
            SetViewsInteractable(false);
            SetFieldInputEnabled(false);

            _isTransitioning = false;
            SetViewsInteractable(true);
            RefreshNavigationState();
        }

        public void DataDestroy()
        {
            _isTransitioning = true;
            SetViewsInteractable(false);

            DOVirtual.DelayedCall(0.3f, () =>
            {
                _isTransitioning = false;
                SetFieldInputEnabled(true);
            }).SetUpdate(true);
        }
    }
}