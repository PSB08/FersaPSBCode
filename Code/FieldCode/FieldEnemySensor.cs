using System.Threading;
using System.Threading.Tasks;
using PSB.Code.BattleCode.UIs;
using UnityEngine;
using UnityEngine.Events;
using Work.PSB.Code.CoreSystem;
using YIS.Code.Modules;

namespace Work.PSB.Code.FieldCode
{
    public class FieldEnemySensor : MonoBehaviour, IModule
    {
        [Header("감지 설정")]
        [SerializeField] private float detectDelay = 1.5f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private BattleEnterContextSO enterContext;
        [SerializeField] private LayerMask playerLayer;
        
        [SerializeField] private EnemyDetectUI detectionUI;

        public UnityEvent OnDetectConfirmed;

        private bool _playerDetected = false;
        private bool _isDetecting = false;
        private bool _battleEnterRaised = false;
        
        private FieldEnemyAngle _view;
        private CancellationTokenSource _detectCTS;
        private HitMessageEffect _hitMessageEffect;
        private ContactFilter2D _contactFilter2D;
        
        private float _lostTimer = 0f;
        private readonly float _lostDelay = 0.2f;

        public Transform CurrentTarget { get; private set; }
        public bool PlayerDetected => _playerDetected;
        public bool IsDetecting => _isDetecting;
        
        private Collider2D[] _hitColliders = new Collider2D[16];

        public void Initialize(ModuleOwner owner)
        {
        }

        private void Awake()
        {
            _view = GetComponent<FieldEnemyAngle>();
            _hitMessageEffect = FindAnyObjectByType<HitMessageEffect>();
            
            _contactFilter2D = new ContactFilter2D();
            _contactFilter2D.useLayerMask = true;
            _contactFilter2D.SetLayerMask(playerLayer);
            _contactFilter2D.useTriggers = true;
        }

        private void Start()
        {
            OnDetectConfirmed.AddListener(_hitMessageEffect.Play);
        }

        private void Update()
        {
            if (TransitionController.IsTransitioning)
            {
                if (_isDetecting || _playerDetected || CurrentTarget != null)
                {
                    ResetDetection();
                    CurrentTarget = null;
                }
                return; 
            }

            DetectTargets();
        }
        
        private void OnDisable()
        {
            StopDetectionInternal();
            OnDetectConfirmed.RemoveListener(_hitMessageEffect.Play);
        }

        private void OnDestroy()
        {
            StopDetectionInternal();
        }

        private void StopDetectionInternal()
        {
            if (_detectCTS != null)
            {
                _detectCTS.Cancel();
                _detectCTS.Dispose();
                _detectCTS = null;
            }

            _isDetecting = false;
            
            _playerDetected = false;

            _battleEnterRaised = false;

            if (_view != null)
            {
                _view.ResetDetectProgress();
                _view.SetDetecting(false);
            }
            
            if (detectionUI != null) detectionUI.Hide();
        }

        public bool IsPlayerInSight(out Transform player)
        {
            player = null;

            int hitCount = Physics2D.OverlapCircle(transform.position,
                _view.ViewDistance, _contactFilter2D, _hitColliders);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hitColliders[i];
                if (hit.CompareTag(playerTag) && IsInView(hit.transform.position))
                {
                    Vector2 dir = (hit.transform.position - transform.position).normalized;
                    float dist = Vector2.Distance(transform.position, hit.transform.position);

                    int wallMask = LayerMask.GetMask("Wall");
                    RaycastHit2D rayHit = Physics2D.Raycast(transform.position, dir, dist, wallMask);

                    if (rayHit.collider == null)
                    {
                        player = hit.transform;
                        CurrentTarget = player;
                        return true;
                    }
                }
            }

            return false;
        }

        private void DetectTargets()
        {
            bool found = IsPlayerInSight(out var player);

            if (found)
            {
                CurrentTarget = player;
                
                _lostTimer = 0f;
                
                if (_playerDetected)
                    return;

                if (_detectCTS == null)
                {
                    _detectCTS = new CancellationTokenSource();
                    _isDetecting = true;

                    _view.ResetDetectProgress();
                    _view.SetDetecting(true); 

                    _ = StartDetectSequence(_detectCTS.Token);
                }
            }
            else
            {
                CurrentTarget = null;
                _lostTimer += Time.deltaTime;

                if (_lostTimer >= _lostDelay)
                {
                    ResetDetection();
                    
                    if (detectionUI != null) detectionUI.Hide();
                }
            }
        }

        private async Task StartDetectSequence(CancellationToken token)
        {
            float elapsed = 0f;
            
            if (detectionUI != null) detectionUI.Show();

            while (true)
            {
                if (token.IsCancellationRequested || !this || !isActiveAndEnabled || !gameObject.activeInHierarchy)
                {
                    _isDetecting = false;
                    _view?.ResetDetectProgress();
                    _view?.SetDetecting(false);
                    return;
                }

                elapsed += Time.deltaTime;

                if (elapsed >= detectDelay)
                {
                    // 이 프레임에 바로 확정
                    _view.SetDetectProgress(1f);

                    Debug.Log($"[Sensor] PlayerDetected TRUE frame={Time.frameCount}");
                    _playerDetected = true;
                    _isDetecting = false;
                    _view.SetDetecting(false);

                    if (!_battleEnterRaised)
                    {
                        _battleEnterRaised = true;
                        Debug.Log("Enemy detect and battle enter");
                        enterContext.Set(BattleEnterBy.Enemy);
                    }

                    return;
                }

                float p = elapsed / detectDelay;
                _view.SetDetectProgress(p);
                if (detectionUI != null) detectionUI.UpdateProgress(p);

                await Task.Yield();
            }
        }

        private bool IsInView(Vector3 targetPos)
        {
            Vector3 dirToTarget = (targetPos - transform.position).normalized;
            Vector3 viewDir = _view.GetViewDirection().normalized;

            float dot = Vector3.Dot(viewDir, dirToTarget);
            float angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;

            return angle < _view.ViewAngle / 2f;
        }

        public void ResetDetection()
        {
            _playerDetected = false;
            _isDetecting = false;

            _detectCTS?.Cancel();
            _detectCTS?.Dispose();
            _detectCTS = null;

            _battleEnterRaised = false;

            _view.ResetDetectProgress();
            _view.SetDetecting(false);
        }
        
    }
}
