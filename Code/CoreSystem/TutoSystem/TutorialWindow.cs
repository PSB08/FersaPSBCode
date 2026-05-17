using System.Collections;
using System.Collections.Generic;
using PSB.Code.BattleCode.UIs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    public class TutorialWindow : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoRawImage;
        [SerializeField] private RenderTexture renderTexture;
        [SerializeField] private Image tutoImage;
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button exitButton;

        [Header("Transition")]
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private float fadeOutDuration = 0.12f;
        [SerializeField] private float fadeInDuration = 0.16f;

        [Header("Page")]
        [SerializeField] private Transform dotParent;
        [SerializeField] private GameObject dotPrefab;

        [Header("Data")]
        [SerializeField] private TutorialDataSO tutorialData;

        [Header("Option")]
        [SerializeField] private bool showOnce = true;

        [SerializeField] private TutorialWindowAnimator animator;

        private int _currentIndex;
        private readonly List<Image> _dots = new();
        private Coroutine _pageTransitionRoutine;
        private bool _isTransitioning;

        private List<TutorialPageSO> Pages => tutorialData?.Pages;
        private string TutorialId => tutorialData != null ? tutorialData.TutorialId : string.Empty;
        private int PageCount => Pages?.Count ?? 0;

        private void Awake()
        {
            animator.Init();

            if (videoPlayer != null)
                videoPlayer.prepareCompleted += OnVideoPrepared;

            if (contentCanvasGroup != null)
                contentCanvasGroup.alpha = 1f;
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
                videoPlayer.prepareCompleted -= OnVideoPrepared;

            if (prevButton != null)
                prevButton.onClick.RemoveListener(OnPrev);

            if (nextButton != null)
                nextButton.onClick.RemoveListener(OnNext);

            if (exitButton != null)
                exitButton.onClick.RemoveListener(HideAction);
        }

        private void Start()
        {
            if (tutorialData == null || PageCount <= 0)
            {
                Debug.LogWarning("[TutorialWindow] tutorialData 또는 pages가 비어있습니다.", this);
                gameObject.SetActive(false);
                return;
            }

            if (showOnce && TutorialArchiveSave.IsViewed(TutorialId))
            {
                gameObject.SetActive(false);
                return;
            }

            InitDots();
            RefreshImmediate();

            if (prevButton != null)
                prevButton.onClick.AddListener(OnPrev);

            if (nextButton != null)
                nextButton.onClick.AddListener(OnNext);

            if (exitButton != null)
                exitButton.onClick.AddListener(HideAction);

            StartCoroutine(ShowCoroutine());
        }

        private void Show()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            SetButtonsInteractable(false);
            animator.PlayShow((bool _) =>
            {
                _isTransitioning = false;
                SetButtonsInteractable(true);
            });
        }

        private void Hide()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            SetButtonsInteractable(false);
            animator.PlayHide((bool _) => { }, () =>
            {
                _isTransitioning = false;
                Time.timeScale = 1f;
                gameObject.SetActive(false);
            });
        }

        private void SetButtonsInteractable(bool value)
        {
            if (!value || _isTransitioning)
            {
                if (prevButton != null) prevButton.interactable = false;
                if (nextButton != null) nextButton.interactable = false;
                if (exitButton != null) exitButton.interactable = false;
                return;
            }

            bool isFirst = _currentIndex == 0;
            bool isLast = _currentIndex == PageCount - 1;

            if (prevButton != null)
                prevButton.interactable = !isFirst;

            if (nextButton != null && nextButton.gameObject.activeSelf)
                nextButton.interactable = !isLast;

            if (exitButton != null && exitButton.gameObject.activeSelf)
                exitButton.interactable = isLast;
        }

        private IEnumerator ShowCoroutine()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            Time.timeScale = 0f;
            Show();
        }

        private void HideAction()
        {
            if (_isTransitioning) return;

            if (showOnce && IsCompleted())
                TutorialArchiveSave.MarkViewed(TutorialId);

            Hide();
        }

        private bool IsCompleted()
        {
            return PageCount > 0 && _currentIndex >= PageCount - 1;
        }

        private void InitDots()
        {
            foreach (Transform c in dotParent)
                Destroy(c.gameObject);

            _dots.Clear();

            for (int i = 0; i < PageCount; i++)
            {
                var dot = Instantiate(dotPrefab, dotParent);
                _dots.Add(dot.GetComponent<Image>());
            }
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

        private void RefreshImmediate()
        {
            if (PageCount <= 0) return;

            var page = Pages[_currentIndex];
            if (page == null) return;

            if (titleText != null)
                titleText.text = page.title;

            if (tutorialText != null)
                tutorialText.text = page.description;

            if (page.videoClip != null)
                PlayVideo(page.videoClip);
            else
                ShowImage(page.image);

            UpdateDots();
        }

        private IEnumerator ChangePageRoutine(int newIndex)
        {
            _isTransitioning = true;
            SetButtonsInteractable(false);

            yield return FadeContent(1f, 0f, fadeOutDuration);

            _currentIndex = newIndex;
            RefreshImmediate();

            yield return FadeContent(0f, 1f, fadeInDuration);

            _isTransitioning = false;
            _pageTransitionRoutine = null;
            SetButtonsInteractable(true);
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

        private void UpdateDots()
        {
            for (int i = 0; i < _dots.Count; i++)
            {
                if (_dots[i] == null) continue;

                _dots[i].color = i == _currentIndex
                    ? Color.white
                    : new Color(1f, 1f, 1f, 0.3f);
            }
        }

        public void OnPrev()
        {
            if (_isTransitioning || _currentIndex <= 0) return;

            if (_pageTransitionRoutine != null)
                StopCoroutine(_pageTransitionRoutine);

            _pageTransitionRoutine = StartCoroutine(ChangePageRoutine(_currentIndex - 1));
        }

        public void OnNext()
        {
            if (_isTransitioning) return;

            if (_currentIndex >= PageCount - 1)
            {
                HideAction();
                return;
            }

            if (_pageTransitionRoutine != null)
                StopCoroutine(_pageTransitionRoutine);

            _pageTransitionRoutine = StartCoroutine(ChangePageRoutine(_currentIndex + 1));
        }
        
    }
}