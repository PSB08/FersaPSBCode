using System;
using System.Collections;
using System.Collections.Generic;
using Code.Scripts.Entities;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.Mechanics.ActionGates;
using PSB.Code.BattleCode.Enemies.Mechanics.Phases;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Feedbacks;
using PSB_Lib.Dependencies;
using UnityEngine;
using YIS.Code.Feedbacks;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Enemies.PhaseBreak
{
    public class EnemyPhaseBreakController : MonoBehaviour, IModule, IEnemyPhaseTransitionGuard, IEnemyActionGate
    {
        //Resources에서 기본 DB를 찾을 때 사용하는 파일명
        private const string DefaultDatabaseResourcePath = "EnemyPhaseBreakDatabase";
        //Wake 상태를 보냈는데 BT Wake가 시작되지 않을 때 기다리는 최대 시간
        private const float WakeBtStartTimeout = 1.5f;
        //Wake 노드가 시작된 뒤에도 완료되지 않을 때 강제로 빠져나오기 위한 최대 시간
        private const float WakeBtTimeout = 12f;
        //HP UI 채우기 완료 콜백이 누락됐을 때 Wake가 계속 멈추지 않게 하는 대기 시간
        private const float HpFillTimeout = 3f;
        
        //적별, 등급별 PhaseBreakProfile을 찾기 위한 DB
        [SerializeField] private EnemyPhaseBreakDatabase phaseBreakDatabase;
        //DB에서 Profile을 못 찾았을 때 사용할 Profile
        [SerializeField] private EnemyPhaseBreakProfile fallbackProfile;
        //DB가 인스펙터에 없으면 Resources에서 자동으로 찾을지 결정
        [SerializeField] private bool loadDatabaseFromResources = true;
        
        private BattleEnemy _enemy;
        private EntityHealth _health;
        private EntityRenderer _renderer;
        private EnemyPhaseBreakProfile _profile; //현재 적에게 적용된 PhaseBreak 설정
        
        //나중에 Wake 때 실행할 실제 페이즈 변경 액션
        private Action _phaseAction;
        
        //Stun 중 재생된 피드백 인스턴스들을 정리하기 위해 저장
        private readonly List<FeedbackPlayer> _stunFeedbacks = new();
        //Wake 중 재생된 피드백 인스턴스들을 정리하기 위해 저장
        private readonly List<FeedbackPlayer> _wakeFeedbacks = new();
        //마무리 피드백 인스턴스들을 정리하기 위해 저장
        private readonly List<FeedbackPlayer> _finishFeedbacks = new();
        
        //Wake 처리 코루틴이 이미 돌고 있는지 추적
        private Coroutine _wakeCo;
        //BT Wake 액션이 완료 여부를 확인할 때 쓰는 플래그
        private bool _wakeDone;
        //BT Wake 액션이 Success, Failure를 결정할 때 쓰는 플래그
        private bool _wakeOk;
        
        public bool HasPendingBreak { get; private set; } //페이즈 변경 액션이 Wake까지 보류되어 있는지
        public bool IsPhaseBreakStunned { get; private set; } //현재 이 적이 페이즈 브레이크 스턴 상태인지
        public bool IsResolving { get; private set; } //Wake 처리 코루틴이 진행 중인지
        public bool WakeDone => _wakeDone; //BT Wake 액션이 Wake 처리 완료를 읽기 위한 값
        public bool WakeOk => _wakeOk; //BT Wake 액션이 Wake 처리 성공 여부를 읽기 위한 값
        public int PhaseTransitionPriority => 1000;
        public int ActionGatePriority => 1000;
        
        //이 적이 PhaseBreak를 사용할 수 있는 기본 조건
        public bool CanUsePhaseBreak
            => _enemy != null && !_enemy.IsDead && _profile != null && _profile.IsEnabled;
        
        //직접 지정 대상에서 막아야 하는지 타겟팅 코드가 읽는 값
        public bool BlocksDirectTarget
            => IsPhaseBreakStunned && BlocksDirect();
        
        //범위 피해 대상에서 막아야 하는지 타겟팅 코드가 읽는 값
        public bool BlocksRangeTarget
            => IsPhaseBreakStunned && BlocksRange();
        
        public void Initialize(ModuleOwner owner)
        {
            _enemy = owner as BattleEnemy;
            _health = owner != null ? owner.GetModule<EntityHealth>() : null;
            _renderer = owner != null ? owner.GetModule<EntityRenderer>() : null;
            if (_health != null)
            {
                //페이즈 경계에 닿았을 때 UI 페이즈 넘김을 바로 하지 않고 Wake 처리까지 보류
                _health.ClearPhaseHold(ShouldHoldPhase);
                _health.SetPhaseHold(ShouldHoldPhase);
            }
            
            SetProfile();
        }
        
        public void RefreshProfile()
        {
            //EnemySO나 DB 설정이 바뀐 뒤 Profile을 다시 찾을 때 호출
            SetProfile();
        }
        
        public bool BeginPhaseBreak(Action resolvePhaseAction)
        {
            //페이즈 경계 도달 시 바로 페이즈를 적용하지 않고 Stun 상태로 먼저 넘기기
            if (!CanUsePhaseBreak || HasPendingBreak || IsResolving)
                return false;
            
            if (!TryProfile(out _))
            {
                //Profile이 없거나 꺼져 있으면 PhaseBreak를 시작하지 X
                return false;
            }
            
            if (!CanSendState(BattleEnemyState.Stun))
            {
                //BT 채널이 준비되지 않았으면 Stun 상태를 보낼 수 없으므로 시작하지 X
                return false;
            }
            
            HasPendingBreak = true; //이제부터 이 적은 페이즈 변경을 Wake까지 보류
            IsPhaseBreakStunned = true; //타겟팅에서 이 적을 제외할 수 있게 스턴 상태로 표시
            _phaseAction = resolvePhaseAction; //실제 페이즈 적용 액션은 Wake 때 실행하기 위해 저장
            
            if (!SendState(BattleEnemyState.Stun))
            {
                //전송 실패 시 보류 상태만 남지 않게 즉시 롤백
                HasPendingBreak = false;
                IsPhaseBreakStunned = false;
                _phaseAction = null;
                return false;
            }
            
            return true;
        }
        
        public bool TryHoldTransition(EnemyPhaseTransitionRequest request,
            Action continueTransition)
        {
            if (request.Context == null ||
                !ReferenceEquals(request.Context.Enemy, _enemy))
            {
                return false;
            }
            
            return BeginPhaseBreak(continueTransition);
        }
        
        public bool ShouldResolveBeforeAction(EnemyActionGateContext context)
        {
            return ReferenceEquals(context.Enemy, _enemy) &&
                   (HasPendingBreak || IsResolving);
        }
        
        public IEnumerator ResolveBeforeAction(EnemyActionGateContext context)
        {
            if (!ReferenceEquals(context.Enemy, _enemy))
                yield break;
            
            yield return ResolveBeforeAttack();
        }
        
        public IEnumerator ResolveBeforeAttack()
        {
            //적 턴 시작 전에 Wake BT 연출, 페이즈 적용, HP UI 채우기를 끝낸 뒤 공격
            if (!HasPendingBreak)
                yield break;
            
            if (IsResolving)
            {
                //이미 다른 호출에서 Wake 처리가 진행 중이면 끝날 때까지 같이 기다리기
                while (IsResolving)
                    yield return null;
                
                yield break;
            }
            
            IsResolving = true; //지금부터 Wake 처리 중임을 표시합니다.
            
            //새 Wake 처리 시작 전 완료, 성공 플래그를 초기화
            _wakeDone = false;
            _wakeOk = false;
            
            if (!TryProfile(out EnemyPhaseBreakProfile profile))
            {
                //Profile이 사라졌으면 보류 액션만 안전하게 처리하고 종료
                ForceCompleteWake(false);
                yield break;
            }
            
            if (!SendState(BattleEnemyState.Wake))
            {
                //Wake 상태를 보낼 수 없으면 pending 상태로 멈추지 않게 강제 완료
                ForceCompleteWake(false);
                yield break;
            }
            
            //Profile 타이밍보다 넉넉한 최대 대기 시간을 계산
            float timeout = Mathf.Max(WakeBtTimeout, profile.WakeDuration + profile.FinishDuration + 5f);
            //Wake BT 시작, 완료 timeout 계산용 누적 시간
            float elapsed = 0f;
            
            while (!_wakeDone)
            {
                //매 프레임 경과 시간을 누적
                elapsed += Time.deltaTime;
                if (_wakeCo == null && elapsed >= WakeBtStartTimeout)
                {
                    //Wake BT 액션이 StartWakeFromBT를 호출하지 않은 상태
                    Debug.LogError($"[EnemyPhaseBreakController] {_enemy?.name} Wake BT sequence did not start.", this);
                    ForceCompleteWake(false);
                    yield break;
                }
                
                if (elapsed >= timeout)
                {
                    //Wake BT 액션이 끝나지 않아 적 턴이 막히는 상황을 방지
                    Debug.LogError($"[EnemyPhaseBreakController] {_enemy?.name} Wake BT sequence did not complete.", this);
                    ForceCompleteWake(false);
                    yield break;
                }
                
                yield return null;
            }
        }
        
        public void PlayStunFromBT()
        {
            //Stun 상태에서 실행되는 공통 피드백은 BT 액션이 이 메서드를 호출해서 재생
            if (!HasPendingBreak)
                return;
            
            if (!TryProfile(out EnemyPhaseBreakProfile profile))
                return;
            
            Stop(_stunFeedbacks);
            Play(profile.StunFeedbacks, _stunFeedbacks);
        }
        
        public bool StartWakeFromBT()
        {
            if (!HasPendingBreak)
            {
                //보류된 페이즈가 없으면 BT 액션이 바로 끝날 수 있게 완료 처리
                _wakeDone = true;
                _wakeOk = true;
                return false;
            }
            
            if (_wakeCo != null)
            {
                //이미 Wake 코루틴이 돌고 있으면 중복 시작하지 X
                return true;
            }
            
            Stop(_stunFeedbacks);
            
            //Wake 코루틴 시작 전 완료, 성공 플래그를 초기화
            _wakeDone = false;
            _wakeOk = false;
            
            //실제 Wake 처리 코루틴을 시작
            _wakeCo = StartCoroutine(WakeRoutine());
            return true;
        }
        
        private IEnumerator WakeRoutine()
        {
            //Wake 연출 완료 후 페이즈 적용, 체력 UI 채우기, 마무리 피드백을 순서대로 처리
            bool success = false;
            
            try
            {
                if (!TryProfile(out EnemyPhaseBreakProfile profile))
                {
                    //Profile이 없으면 피드백은 못 틀어도 보류된 페이즈 액션은 처리
                    ApplyPendingPhase();
                    yield break;
                }
                
                //Wake 단계 피드백을 실행합니다.
                Play(profile.WakeFeedbacks, _wakeFeedbacks);
                
                //Wake 애니메이션과 연출 최소 대기 시간
                float wakeTime = profile.WakeDuration;
                while (wakeTime > 0f)
                {
                    wakeTime -= Time.deltaTime;
                    yield return null;
                }
                
                //Wake가 끝났으니 Wake, Stun 피드백을 먼저 정리
                Stop(_wakeFeedbacks);
                Stop(_stunFeedbacks);
                
                //완전히 깨어난 뒤 보류된 페이즈 액션을 적용하고 HP UI 채우기 완료 콜백을 받음
                bool hpAnimationDone = !profile.WaitForHpFill;
                ApplyPendingPhase(() => hpAnimationDone = true);
                
                //HP UI 채우기 콜백 누락 여부를 판단하기 위한 누적 시간
                float hpFillWaitTime = 0f;
                while (profile.WaitForHpFill && !hpAnimationDone)
                {
                    hpFillWaitTime += Time.deltaTime;
                    
                    if (hpFillWaitTime >= HpFillTimeout)
                    {
                        //HP UI 완료 콜백이 누락돼도 Wake가 막히지 않게 강제로 다음 흐름으로 넘김
                        Debug.LogWarning($"[EnemyPhaseBreakController] {_enemy?.name} HP fill animation timeout. Force continuing Wake.", this);
                        hpAnimationDone = true;
                    }
                    
                    yield return null;
                }
                
                //행동 시작 전 마무리 피드백을 실행
                Play(profile.FinishFeedbacks, _finishFeedbacks);
                
                //마무리 피드백이 보일 시간을 기다리기
                float finishTime = profile.FinishDuration;
                if (finishTime > 0f)
                    yield return new WaitForSeconds(finishTime);
                
                //여기까지 오면 정상 Wake 처리
                success = true;
            }
            finally
            {
                //성공, 실패와 상관없이 상태와 피드백을 정리
                CompleteWake(success);
            }
        }
        
        private void SetProfile()
        {
            //DB가 비어 있고 자동 로드 옵션이 켜져 있으면 Resources에서 찾기
            if (phaseBreakDatabase == null && loadDatabaseFromResources)
                phaseBreakDatabase = Resources.Load<EnemyPhaseBreakDatabase>(DefaultDatabaseResourcePath);
            
            //DB가 있으면 현재 적 EnemySO에 맞는 Profile을 찾기
            _profile = phaseBreakDatabase != null && _enemy != null
                ? phaseBreakDatabase.Resolve(_enemy.enemySO)
                : null;
            
            //DB에서 못 찾은 경우 fallbackProfile을 사용
            if (_profile == null)
                _profile = fallbackProfile;
        }
        
        private bool TryProfile(out EnemyPhaseBreakProfile profile)
        {
            //현재 캐싱된 Profile을 out으로 넘기기
            profile = _profile;
            
            if (profile != null && profile.IsEnabled)
            {
                //Profile이 있고 활성화되어 있으면 사용 가능
                return true;
            }
            
            Debug.LogError($"[EnemyPhaseBreakController] {_enemy?.name} has no enabled EnemyPhaseBreakProfile.", this);
            return false;
        }
        
        private bool BlocksDirect()
        {
            //Profile이 없으면 보수적으로 직접 지정 차단 상태로 취급
            return _profile == null || _profile.BlockDirectTarget;
        }
        
        private bool BlocksRange()
        {
            //Profile이 없으면 보수적으로 범위 피해 차단 상태로 취급
            return _profile == null || _profile.BlockRangeTarget;
        }
        
        private bool ShouldHoldPhase()
        {
            //EntityHealth가 페이즈 UI 전환을 보류할지 물어볼 때 사용하는 조건
            return CanUsePhaseBreak;
        }
        
        private void CompleteWake(bool success)
        {
            Stop(_wakeFeedbacks); //새 Wake 처리 시작 전 성공 플래그를 초기화
            Stop(_stunFeedbacks); //Stun 지속 피드백도 Wake 완료 시점에 정리
            Stop(_finishFeedbacks); //Finish 피드백도 남지 않게 정리
            
            HasPendingBreak = false; //더 이상 보류된 페이즈 브레이크가 없음을 표시
            IsPhaseBreakStunned = false; //타겟팅 차단용 스턴 상태를 해제
            IsResolving = false; //Wake 처리 중 상태를 해제
            _wakeCo = null; //코루틴 참조를 비워 다음 Wake 처리가 가능하게
            _wakeOk = success; //BT 액션이 읽을 성공 여부를 저장
            
            _wakeDone = true; //BT 액션이 Running을 끝낼 수 있게 완료 플래그
        }
        
        private void EnsurePlannedIntent()
        {
            EnemyAttack attack = _enemy != null ? _enemy.GetModule<EnemyAttack>() : null;

            if (attack == null || attack.HasPlanned)
                return;

            attack.PlanNextIntent();
        }
        
        private void ForceCompleteWake(bool success)
        {
            if (_wakeCo != null)
            {
                //진행 중인 Wake 코루틴이 있으면 강제로 정지
                StopCoroutine(_wakeCo);
                _wakeCo = null;
            }
            
            ApplyPendingPhase(); //그래도 페이즈 액션과 HP UI 보류는 풀어서 게임 진행이 막히지 않게
            CompleteWake(success); //실패 상태로라도 Wake 처리를 완료 처리
        }
        
        private void ApplyPendingPhase(Action onHpDone = null)
        {
            //보류해둔 페이즈 액션을 실행, 새 페이즈 HP UI가 0에서 차오르게 넘기기
            Action phaseAction = _phaseAction;
            _phaseAction = null;
            phaseAction?.Invoke();
            
            if (_health != null)
                _health.ResolvePhaseAdvance(onHpDone);
            else
                onHpDone?.Invoke();
            
            EnsurePlannedIntent();
        }
        
        private bool CanSendState(BattleEnemyState state)
        {
            if (_enemy == null)
            {
                Debug.LogError($"[EnemyPhaseBreakController] Cannot send {state}: BattleEnemy is missing.", this);
                return false;
            }
            
            if (_enemy.CanSendBTState())
                return true;
            
            Debug.LogError($"[EnemyPhaseBreakController] Cannot send {state}: BT state channel is missing.", this);
            return false;
        }
        
        private bool SendState(BattleEnemyState state)
        {
            if (_enemy == null)
            {
                Debug.LogError($"[EnemyPhaseBreakController] Cannot send {state}: BattleEnemy is missing.", this);
                return false;
            }
            
            return _enemy.TrySendBTState(state);
        }
        
        private void Play(EnemyPhaseBreakFeedbackEntry[] entries, List<FeedbackPlayer> instances)
        {
            if (entries == null || entries.Length == 0)
            {
                return;
            }
            
            foreach (EnemyPhaseBreakFeedbackEntry entry in entries)
            {
                if (entry == null || entry.FeedbackPrefab == null)
                {
                    Debug.LogError($"[EnemyPhaseBreakController] {_enemy?.name} has an empty phase break feedback entry.", this);
                    continue;
                }
                
                //렌더러 찾기
                Renderer targetRenderer = GetRenderer();
                //위치 찾기
                Transform root = GetRoot(targetRenderer);
                //프리팹 생성
                FeedbackPlayer instance = Instantiate(entry.FeedbackPrefab, root.position,
                    Quaternion.identity, entry.ParentToEnemy ? root : null);
                Injector.Instance?.InjectTo(instance.gameObject);
                
                if (entry.ParentToEnemy)
                {
                    instance.transform.localPosition = Vector3.zero;
                }
                
                //MaterialFloatFeedback, StunEffectFeedback 같은 피드백에 실제 적 대상을 전달
                SetTargets(instance, targetRenderer, root);
                //피드백 재생
                instance.PlayFeedbacks();
                //나중에 Stop, Destroy할 수 있게 인스턴스 목록에 저장
                instances.Add(instance);
            }
        }
        
        private Renderer GetRenderer()
        {
            if (_renderer != null && _renderer.SpriteRenderer != null)
            {
                return _renderer.SpriteRenderer;
            }
            
            return GetComponentInChildren<Renderer>();
        }
        
        private Transform GetRoot(Renderer targetRenderer)
        {
            if (targetRenderer != null)
            {
                return targetRenderer.transform;
            }
            
            return transform;
        }
        
        private void SetTargets(FeedbackPlayer instance, Renderer targetRenderer, Transform root)
        {
            if (instance == null)
            {
                return;
            }
            
            MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IRendererTargetFeedback targetFeedback)
                {
                    targetFeedback.SetTargetRenderer(targetRenderer);
                }
                
                if (behaviour is ITransformTargetFeedback transformFeedback)
                {
                    transformFeedback.SetTarget(root);
                }
            }
        }
        
        private void Stop(List<FeedbackPlayer> instances)
        {
            if (instances == null)
            {
                return;
            }
            
            foreach (FeedbackPlayer instance in instances)
            {
                if (instance == null)
                {
                    continue;
                }
                
                instance.StopFeedbacks();
                Destroy(instance.gameObject);
            }
            
            instances.Clear();
        }
        
        private void OnDestroy()
        {
            if (_health != null)
            {
                //EntityHealth에 등록했던 페이즈 보류 콜백을 해제
                _health.ClearPhaseHold(ShouldHoldPhase);
            }
            
            if (_wakeCo != null)
            {
                //오브젝트가 사라질 때 진행 중인 Wake 코루틴이 남지 않게 멈추기
                StopCoroutine(_wakeCo);
            }
            
            //생성해 둔 모든 PhaseBreak 피드백을 정리
            Stop(_stunFeedbacks);
            Stop(_wakeFeedbacks);
            Stop(_finishFeedbacks);
        }
        
    }
}
