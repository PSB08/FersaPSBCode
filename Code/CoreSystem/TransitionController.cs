using System;
using UnityEngine;
using UnityEngine.UI;
using Work.PSB.Code.FieldCode.MapSaves;

namespace Work.PSB.Code.CoreSystem
{
    [RequireComponent(typeof(Image))]
    [DefaultExecutionOrder(-5000)]
    public class TransitionController : MonoBehaviour
    {
        private static readonly int CutOff = Shader.PropertyToID("_CutOff");
        private static readonly int EdgeSmoothing = Shader.PropertyToID("_EdgeSmoothing");
        public static bool IsTransitioning { get; private set; }

        [Header("Scene")]
        public string nextScene;

        [Header("Mask Speed")]
        [SerializeField] private float closeSpeed = 2.2f;
        [SerializeField] private float revealSpeed = 2.0f;

        [SerializeField] private float cooldownAfterFinish = 0.3f;
        [SerializeField] private float maxUnscaledDelta = 1f / 30f;
        [SerializeField] private float holdClosedSeconds = 0.15f;

        [SerializeField] private float prerollSeconds = 0.08f;
        [SerializeField, Range(0.01f, 1f)]
        private float prerollSpeedMultiplier = 0.15f;

        public event Action OnClosed;
        private bool _closedFired;

        private Image _image;
        private Material _mat;

        private bool _shouldReveal;
        private bool _isPlaying;
        private bool _sceneLoadTriggered;

        private float _cooldownTimer;
        private float _holdTimer;
        private float _prerollTimer;
        private float _lastUnscaledTime;
        
        private bool _hasQueuedTransitionRequest;
        private string _queuedSceneName;

        private const float OpenValue = 1.1f;
        private float ClosedValue => -0.1f - Mathf.Max(0f, _mat.GetFloat(EdgeSmoothing));

        private void Awake()
        {
            _image = GetComponent<Image>();

            _mat = Instantiate(_image.material);
            _image.material = _mat;

            transform.SetAsLastSibling();
        }

        private void Start()
        {
            _lastUnscaledTime = Time.unscaledTime;

            bool pending = SceneLoader.ConsumePendingReveal();

            if (pending)
            {
                _image.enabled = true;
                _mat.SetFloat(CutOff, ClosedValue);

                _shouldReveal = true;
                _isPlaying = true;
                _sceneLoadTriggered = false;
                _closedFired = false;
            }
            else
            {
                _mat.SetFloat(CutOff, OpenValue);
                _image.enabled = false;

                _shouldReveal = false;
                _isPlaying = false;
                _sceneLoadTriggered = false;
                _closedFired = false;
            }

            IsTransitioning = _isPlaying;
            
            _cooldownTimer = 0f;
            _holdTimer = 0f;
            _prerollTimer = 0f;

            _hasQueuedTransitionRequest = false;
            _queuedSceneName = null;
        }

        private void Update()
        {
            IsTransitioning = _isPlaying;
            
            if (!_isPlaying)
            {
                if (_cooldownTimer > 0f)
                    _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.unscaledDeltaTime);
                
                if (_cooldownTimer <= 0f && _hasQueuedTransitionRequest)
                {
                    StartCloseTransition(_queuedSceneName);
                    _hasQueuedTransitionRequest = false;
                    _queuedSceneName = null;
                }

                return;
            }

            TickTransition();
        }

        private void OnDestroy()
        {
            IsTransitioning = false;
        }

        public void Transition()
        {
            Transition(nextScene);
        }

        public void Transition(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            nextScene = sceneName;

            if (_isPlaying && !_shouldReveal)
            {
                nextScene = sceneName;
                _queuedSceneName = null;
                _hasQueuedTransitionRequest = false;
                return;
            }
            
            if (_isPlaying && _shouldReveal)
            {
                StartCloseTransition(sceneName);
                return;
            }
            
            if (_cooldownTimer > 0f)
            {
                _hasQueuedTransitionRequest = true;
                _queuedSceneName = sceneName;
                return;
            }
            
            StartCloseTransition(sceneName);
        }

        private void StartCloseTransition(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            nextScene = sceneName;

            _shouldReveal = false;
            _isPlaying = true;
            _sceneLoadTriggered = false;
            _closedFired = false;

            _image.enabled = true;
            
            _mat.SetFloat(CutOff, OpenValue);

            _holdTimer = 0f;
            _prerollTimer = 0f;
            _cooldownTimer = 0f;
            _lastUnscaledTime = Time.unscaledTime;
        }

        private void TickTransition()
        {
            float now = Time.unscaledTime;
            float dt = now - _lastUnscaledTime;
            _lastUnscaledTime = now;

            if (dt < 0f) dt = 0f;
            if (dt > maxUnscaledDelta) dt = maxUnscaledDelta;

            if (_shouldReveal)
            {
                float v = Mathf.MoveTowards(_mat.GetFloat(CutOff), OpenValue, revealSpeed * dt);
                _mat.SetFloat(CutOff, v);

                if (Mathf.Approximately(v, OpenValue))
                {
                    _image.enabled = false;
                    FinishTransition();
                }
            }
            else
            {
                float speedMul = 1f;
                if (_prerollTimer < prerollSeconds)
                {
                    _prerollTimer += dt;
                    speedMul = prerollSpeedMultiplier;
                }

                float target = ClosedValue;
                float v = Mathf.MoveTowards(_mat.GetFloat(CutOff), target, closeSpeed * speedMul * dt);
                _mat.SetFloat(CutOff, v);

                if (Mathf.Approximately(v, target))
                {
                    if (!_closedFired)
                    {
                        _closedFired = true;
                        OnClosed?.Invoke();
                    }

                    _holdTimer += dt;

                    if (_holdTimer >= holdClosedSeconds && !_sceneLoadTriggered)
                    {
                        _sceneLoadTriggered = true;
                        SceneLoader.LoadScene(nextScene);
                        FinishTransition();
                    }
                }
                else
                {
                    _holdTimer = 0f;
                }
            }
        }

        private void FinishTransition()
        {
            _isPlaying = false;
            _cooldownTimer = cooldownAfterFinish;
        }
        
    }
}