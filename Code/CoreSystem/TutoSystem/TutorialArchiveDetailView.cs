using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    public class TutorialArchiveDetailView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject detailPanelObject;
        [SerializeField] private GameObject emptyStateObject;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoRawImage;
        [SerializeField] private RenderTexture renderTexture;
        [SerializeField] private Image tutoImage;
        [SerializeField] private TextMeshProUGUI tutorialText;

        [Header("Transition")]
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private float fadeOutDuration = 0.12f;
        [SerializeField] private float fadeInDuration = 0.16f;

        [Header("Navigation")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        [Header("Dots")]
        [SerializeField] private Transform dotParent;
        [SerializeField] private GameObject dotPrefab;

        private readonly List<Image> _dots = new();
        private Coroutine _pageTransitionRoutine;
        private TutorialDataSO _currentTutorial;
        private int _currentPageIndex;

        private bool _canPrev;
        private bool _canNext;
        private bool _isInteractable = true;

        public event Action OnPrevClicked;
        public event Action OnNextClicked;

        private void Awake()
        {
            if (videoPlayer != null)
                videoPlayer.prepareCompleted += OnVideoPrepared;

            if (contentCanvasGroup != null)
                contentCanvasGroup.alpha = 1f;
        }

        private void OnEnable()
        {
            if (prevButton != null)
                prevButton.onClick.AddListener(HandlePrevClicked);

            if (nextButton != null)
                nextButton.onClick.AddListener(HandleNextClicked);
        }

        private void OnDisable()
        {
            if (prevButton != null)
                prevButton.onClick.RemoveListener(HandlePrevClicked);

            if (nextButton != null)
                nextButton.onClick.RemoveListener(HandleNextClicked);
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
                videoPlayer.prepareCompleted -= OnVideoPrepared;
        }

        public void SetInteractable(bool value)
        {
            _isInteractable = value;
            ApplyNavigationState();
        }

        public void SetEmptyState(bool empty)
        {
            if (detailPanelObject != null)
                detailPanelObject.SetActive(!empty);

            if (emptyStateObject != null)
                emptyStateObject.SetActive(empty);
        }

        public void SetNavigation(bool canPrev, bool canNext)
        {
            _canPrev = canPrev;
            _canNext = canNext;
            ApplyNavigationState();
        }

        private void ApplyNavigationState()
        {
            if (prevButton != null)
                prevButton.interactable = _isInteractable && _canPrev;

            if (nextButton != null)
                nextButton.interactable = _isInteractable && _canNext;
        }

        private void ClearRenderTexture()
        {
            if (renderTexture == null) return;

            var prev = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = prev;
        }

        private void SetVideoVisible(bool visible)
        {
            if (videoRawImage == null) return;
            videoRawImage.enabled = visible;
        }

        private void StopAndClearVideo()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.clip = null;
            }

            SetVideoVisible(false);
            ClearRenderTexture();
        }

        private void PlayVideo(VideoClip clip)
        {
            if (videoPlayer == null || clip == null) return;

            StopAndClearVideo();

            if (tutoImage != null)
            {
                tutoImage.sprite = null;
                tutoImage.enabled = false;
            }

            videoPlayer.clip = clip;
            videoPlayer.Prepare();
        }

        private void ShowImage(Sprite sprite)
        {
            StopAndClearVideo();

            if (tutoImage != null)
            {
                tutoImage.sprite = sprite;
                tutoImage.enabled = sprite != null;
            }
        }

        private void OnVideoPrepared(VideoPlayer vp)
        {
            SetVideoVisible(true);
            vp.Play();
        }

        public void ShowPage(TutorialDataSO tutorial, int pageIndex)
        {
            if (tutorial == null || tutorial.PageCount <= 0)
            {
                Clear();
                return;
            }

            pageIndex = Mathf.Clamp(pageIndex, 0, tutorial.PageCount - 1);

            bool sameTutorial = _currentTutorial == tutorial;
            bool samePage = sameTutorial && _currentPageIndex == pageIndex;

            _currentTutorial = tutorial;

            if (samePage)
            {
                RefreshImmediate();
                return;
            }

            if (contentCanvasGroup == null || !gameObject.activeInHierarchy)
            {
                _currentPageIndex = pageIndex;
                RefreshImmediate();
                return;
            }

            if (_pageTransitionRoutine != null)
                StopCoroutine(_pageTransitionRoutine);

            _pageTransitionRoutine = StartCoroutine(ChangePageRoutine(pageIndex));
        }

        private void RefreshImmediate()
        {
            if (_currentTutorial == null || _currentTutorial.PageCount <= 0)
            {
                Clear();
                return;
            }

            var page = _currentTutorial.GetPage(_currentPageIndex);
            if (page == null)
            {
                Clear();
                return;
            }

            if (titleText != null)
                titleText.text = string.IsNullOrEmpty(page.title) ? _currentTutorial.DisplayName : page.title;

            if (tutorialText != null)
                tutorialText.text = page.description;

            if (page.videoClip != null)
                PlayVideo(page.videoClip);
            else
                ShowImage(page.image);

            BuildDots(_currentTutorial.PageCount);
            UpdateDots(_currentPageIndex);

            ApplyNavigationState();
        }

        private IEnumerator ChangePageRoutine(int newPageIndex)
        {
            yield return FadeContent(1f, 0f, fadeOutDuration);

            _currentPageIndex = newPageIndex;
            RefreshImmediate();

            yield return FadeContent(0f, 1f, fadeInDuration);

            _pageTransitionRoutine = null;
        }

        private IEnumerator FadeContent(float from, float to, float duration)
        {
            if (contentCanvasGroup == null)
                yield break;

            contentCanvasGroup.alpha = from;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                contentCanvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            contentCanvasGroup.alpha = to;
        }

        public void Clear()
        {
            if (titleText != null)
                titleText.text = string.Empty;

            if (tutorialText != null)
                tutorialText.text = string.Empty;

            StopAndClearVideo();

            if (tutoImage != null)
            {
                tutoImage.sprite = null;
                tutoImage.enabled = false;
            }

            ClearDots();
            _currentTutorial = null;
            _currentPageIndex = 0;
            _canPrev = false;
            _canNext = false;

            if (contentCanvasGroup != null)
                contentCanvasGroup.alpha = 1f;

            ApplyNavigationState();
        }

        private void HandlePrevClicked()
        {
            OnPrevClicked?.Invoke();
        }

        private void HandleNextClicked()
        {
            OnNextClicked?.Invoke();
        }

        private void BuildDots(int count)
        {
            ClearDots();

            for (int i = 0; i < count; i++)
            {
                var dot = Instantiate(dotPrefab, dotParent);
                var img = dot.GetComponent<Image>();
                _dots.Add(img);
            }
        }

        private void UpdateDots(int currentIndex)
        {
            for (int i = 0; i < _dots.Count; i++)
            {
                if (_dots[i] == null)
                    continue;

                _dots[i].color = i == currentIndex
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.3f);
            }
        }

        private void ClearDots()
        {
            if (dotParent == null)
                return;

            foreach (Transform child in dotParent)
                Destroy(child.gameObject);

            _dots.Clear();
        }
        
    }
}