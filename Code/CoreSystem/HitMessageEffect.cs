using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Work.PSB.Code.CoreSystem
{
    public class HitMessageEffect : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private RectTransform messageRect;

        [Header("Motion")]
        [SerializeField] private float offscreenX = 1200f;
        [SerializeField] private float textInDuration = 0.5f;
        [SerializeField] private float textOutDuration = 0.5f;
        [SerializeField] private Ease textInEase = Ease.OutCirc;
        [SerializeField] private Ease textOutEase = Ease.InCirc;

        [Header("Zoom")]
        [SerializeField] private CinemachineCamera cinemachine;
        [SerializeField] private PixelPerfectCamera pixelPerfect;
        [SerializeField] private float zoomInSize = 5f;
        [SerializeField] private float zoomInDuration = 0.5f;
        [SerializeField] private Ease zoomInEase = Ease.OutCirc;

        [Header("Volume")]
        [SerializeField] private VolumeProfile volume;
        [SerializeField] private new Light2D light;

        [Header("Lens Distortion")]
        [SerializeField] private float distortionTarget = -1f;
        [SerializeField] private float distortionDuration = 1f;

        [Header("Color")]
        [SerializeField] private Color enemyVignetteColor = new Color(1f, 0f, 0f, 1f);
        [SerializeField] private Color enemyLightColor = new Color(1f, 0.45f, 0.45f, 1f);

        [Header("Transition")]
        [SerializeField] private TransitionController transitionController;
        [SerializeField] private string sceneName = "BattleScene";
        [SerializeField] private float transitionLeadSeconds = 1.4f;

        //트윈
        private Tween _leadTween;
        private const string TweenId = "HitMessageFX";

        private static readonly AnimationCurve AccelCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0.2f, 0.2f),
            new Keyframe(0.6f, 0.25f, 1.5f, 1.5f),
            new Keyframe(1f, 1f, 4f, 0f)
        );

        //트랜지션 시작과 복구 여부
        private bool _transitionStarted;
        private bool _restoreQueued;

        //픽셀, 볼륨, 라이트 등 복구용도
        private bool _hadPp;
        private bool _hasVignette;
        private bool _hasLight;

        private Vignette _vignette;
        private Light2D _light2D;

        private Color _originalVignetteColor;
        private Color _originalLightColor;
        
        private LensDistortion _distortion;
        private Bloom _bloom;
        private ScreenSpaceLensFlare _lensFlare;
        
        private void Awake()
        {
            if (volume != null)
            {
                volume.TryGet(out _vignette);
                volume.TryGet(out _distortion);
                volume.TryGet(out _bloom);
                volume.TryGet(out _lensFlare);
            }
            _light2D = light;

            if (transitionController != null)
                transitionController.OnClosed += RestoreAfterClosed;
        }

        private void OnDestroy()
        {
            if (transitionController != null)
                transitionController.OnClosed -= RestoreAfterClosed;

            _leadTween?.Kill(false);
        }

        public void Play()
        {
            if (messageRect == null)
            {
                Debug.LogError("[HitMessageEffect] messageRect가 비어 있습니다.");
                return;
            }
            
            _transitionStarted = false;
            _restoreQueued = true;

            Time.timeScale = 0.5f;

            DOTween.Kill(TweenId);
            _leadTween?.Kill(false);
            _leadTween = null;

            messageRect.DOKill();
            if (cinemachine != null) DOTween.Kill(cinemachine, complete: false);

            if (transitionController != null && transitionLeadSeconds > 0f)
            {
                _leadTween = DOVirtual.DelayedCall(transitionLeadSeconds, StartTransitionIfNeeded)
                    .SetUpdate(true)
                    .SetId(TweenId);
            }
            
            _hadPp = pixelPerfect != null && pixelPerfect.enabled;
            if (pixelPerfect != null) pixelPerfect.enabled = false;
            
            if (volume != null)
            {
                if (_vignette == null) volume.TryGet(out _vignette);
            }
            _light2D ??= light;

            Bloom bloom = null;
            if (volume != null && volume.TryGet(out Bloom b) && b != null)
            {
                bloom = b;
                bloom.intensity.value = 5f;
            }

            if (volume != null && volume.TryGet(out ScreenSpaceLensFlare lensFlare) && lensFlare != null)
            {
                DOTween.To(() => lensFlare.intensity.value, x => lensFlare.intensity.value = x, 15f, 0.1f)
                    .SetEase(Ease.OutCirc)
                    .SetId(TweenId)
                    .OnComplete(() =>
                    {
                        DOTween.To(() => lensFlare.intensity.value, x => lensFlare.intensity.value = x, 40f, 1.3f)
                            .SetEase(Ease.OutCirc)
                            .SetId(TweenId);
                    });
            }
            
            _hasVignette = _vignette != null;
            if (_hasVignette)
            {
                _originalVignetteColor = _vignette.color.value;
                _vignette.color.value = enemyVignetteColor;
            }

            _hasLight = _light2D != null;
            if (_hasLight)
            {
                _originalLightColor = _light2D.color;
                _light2D.color = enemyLightColor;
            }
            
            messageRect.anchoredPosition = new Vector2(-offscreenX, messageRect.anchoredPosition.y);
            messageRect.DOAnchorPosX(0f, textInDuration)
                .SetEase(textInEase)
                .SetId(TweenId)
                .OnComplete(() =>
                {
                    messageRect.DOAnchorPosX(offscreenX, textOutDuration)
                        .SetEase(textOutEase)
                        .SetId(TweenId);
                });
            
            if (cinemachine != null)
            {
                DOTween.To(() => cinemachine.Lens.OrthographicSize,
                           x => cinemachine.Lens.OrthographicSize = x,
                           zoomInSize,
                           zoomInDuration)
                       .SetEase(zoomInEase)
                       .SetTarget(cinemachine)
                       .SetId(TweenId);
            }
            
            LensDistortion distortion = null;
            bool hasDistortion = volume != null && volume.TryGet(out distortion) && distortion != null;

            if (hasDistortion)
            {
                distortion.intensity.value = 0f;

                DOTween.To(() => distortion.intensity.value,
                           x => distortion.intensity.value = x,
                           distortionTarget,
                           distortionDuration)
                       .SetEase(AccelCurve)
                       .SetId(TweenId)
                       .OnComplete(StartTransitionIfNeeded);
            }
            else
            {
                StartTransitionIfNeeded();
            }
        }

        private void StartTransitionIfNeeded()
        {
            if (_transitionStarted) return;
            if (transitionController == null) return;

            _transitionStarted = true;
            transitionController.nextScene = sceneName;
            transitionController.Transition();
        }
        
        private void RestoreAfterClosed()
        {
            if (!_restoreQueued) return;
            _restoreQueued = false;

            DOTween.Kill(TweenId);
            _leadTween?.Kill(false);
            _leadTween = null;

            Time.timeScale = 1f;

            if (_distortion != null) _distortion.intensity.value = 0f;
            if (_bloom != null) _bloom.intensity.value = 0f;
            if (_lensFlare != null) _lensFlare.intensity.value = 0f;

            if (_hasVignette && _vignette != null)
                _vignette.color.value = _originalVignetteColor;

            if (_hasLight && _light2D != null)
                _light2D.color = _originalLightColor;

            if (pixelPerfect != null)
                pixelPerfect.enabled = _hadPp;
        }

        public void Stop()
        {
            DOTween.Kill(TweenId);
            _leadTween?.Kill(false);
            _leadTween = null;

            messageRect?.DOKill();
            if (cinemachine != null) DOTween.Kill(cinemachine, complete: false);

            if (volume != null && volume.TryGet(out LensDistortion distortion) && distortion != null)
                distortion.intensity.value = 0f;
        }
        
    }
}