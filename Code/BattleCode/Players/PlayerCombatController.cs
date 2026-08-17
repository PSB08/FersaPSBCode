using CIW.Code;
using System;
using System.Collections;
using System.Collections.Generic;
using CIW.Code.Entities;
using CIW.Code.System.Events;
using DG.Tweening;
using PSB.Code.BattleCode.Allies;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.Feedbacks;
using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSB_Lib.StatSystem;
using PSW.Code.EventBus;
using UnityEngine;
using Work.YIS.Code.Skills;
using YIS.Code.Combat;
using YIS.Code.CoreSystem.Skills;
using YIS.Code.Defines;
using YIS.Code.Events;
using YIS.Code.Modules;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Players
{
    public class PlayerCombatController : MonoBehaviour, IModule
    {
        [Header("Refs")]
        [SerializeField] private StatSO procChanceStat;
        [SerializeField] private PlayerSkillsCache skillsCache;
        [SerializeField] private SkillImpactFeedbackPlayer strongImpactFeedbackPlayer;

        [Header("Safety Timeouts")]
        [SerializeField, Min(0f)] private float playerAttackHitTimeout = 1.2f;
        [SerializeField, Min(0f)] private float playerAttackEndTimeout = 1.5f;

        private BattlePlayer _player;
        private PlayerTargetSelector _selector;
        private PlayerSkillExecutor _skillExecutor;
        EntityAnimatorTrigger _animTrigger;

        private Coroutine _attackCo;
        private bool _turnEnded;
        private SkillEnum[] _used;

        [Inject] private PoolManagerMono _poolManager;
        [Inject] private BattleEnemyManager _enemyManager;
        [Inject] private BattleAllyManager _allyManager;
        [Inject] private BattleSkillSystem _battleSkillSystem;

        private readonly HashSet<SkillEnum> _executedThisTurn = new HashSet<SkillEnum>();
        private readonly Dictionary<SkillEnum, int> _cooldowns = new Dictionary<SkillEnum, int>();

        public PlayerSkillExecutor SkillExecutor => _skillExecutor;

        public void Initialize(ModuleOwner owner)
        {
            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);

            _player = owner as BattlePlayer;
            if (_player != null)
            {
                _selector = _player.GetModule<PlayerTargetSelector>();
                _animTrigger = _player.GetModule<EntityAnimatorTrigger>();
            }

            if (strongImpactFeedbackPlayer != null)
                strongImpactFeedbackPlayer.Initialize(_poolManager);
        }

        private void OnEnable()
        {
            Bus<OnAttackEvent>.OnEvent += OnAttack;
            Bus<OnVoidEndActionEvent>.OnEvent += OnVoidEndAction;
        }

        private void OnDisable()
        {
            Bus<OnAttackEvent>.OnEvent -= OnAttack;
            Bus<OnVoidEndActionEvent>.OnEvent -= OnVoidEndAction;

            if (strongImpactFeedbackPlayer != null)
                strongImpactFeedbackPlayer.ResetAllChainingAttack();

            if (_attackCo != null)
            {
                StopCoroutine(_attackCo);
                _attackCo = null;
            }
        }

        public void OnPlayerTurnStarted()
        {
            _turnEnded = false;
            _attackCo = null;

            if (_cooldowns.Count == 0) return;

            var runningIds = new List<SkillEnum>(_cooldowns.Keys);

            for (int i = 0; i < runningIds.Count; i++)
            {
                SkillEnum id = runningIds[i];
                int remainingTurn = _cooldowns[id] - 1;
                if (remainingTurn > 0)
                    _cooldowns[id] = remainingTurn;
                else
                    _cooldowns.Remove(id);
            }
        }

        private void OnAttack(OnAttackEvent evt)
        {
            if (!isActiveAndEnabled) return;
            if (_player == null || _selector == null) return;

            if (_attackCo != null) StopCoroutine(_attackCo);
            _attackCo = StartCoroutine(CoExecute(evt));
        }

        private IEnumerator CoExecute(OnAttackEvent evt)
        {
            _turnEnded = false;
            _executedThisTurn.Clear();

            //필수 참조가 없으면 즉시 턴 종료
            if (skillsCache == null || _poolManager == null || _enemyManager == null || _player.IsFreeze)
            {
                EndTurnSafely();
                yield break;
            }
            
            if (_skillExecutor != null)
            {
                _skillExecutor.Dispose();
            }

            if (strongImpactFeedbackPlayer != null)
                strongImpactFeedbackPlayer.Initialize(_poolManager);

            //이번 턴에 사용할 SkillExecutor 생성
            _skillExecutor = new PlayerSkillExecutor(
                skillsCache, _poolManager, _enemyManager, 
                _selector, _player, procChanceStat, deferDamage: false,
                _battleSkillSystem
            );

            //이미 선적용한 PARTS 스킬을 기록해서 같은 버프가 두 번 실행되는 방지하기 위한 집합
            var preAppliedParts = new HashSet<SkillEnum>();

            _used = evt.SkillIds;
            Vector3 startPos = _player.transform.position;
            
            bool isDashed = false;

            try
            {
                for (int i = 0; i < evt.SkillIds.Length; i++)
                {
                    SkillEnum id = evt.SkillIds[i];

                    //현재 스킬이 체인으로 실행되는지 여부
                    bool isChain = (evt.ChainFlags != null && i < evt.ChainFlags.Length) && evt.ChainFlags[i];

                    //체인 그룹의 시작 지점인지 판단
                    bool isGroupStart = (i == 0) || !isChain;

                    if (isGroupStart)
                    {
                        int groupStart = i;
                        int groupEnd = i;

                        //현재 인덱스부터, 체인으로 이어지는 마지막 스킬까지 탐색
                        while (groupEnd + 1 < evt.SkillIds.Length)
                        {
                            bool gNextIsChain =
                                (evt.ChainFlags != null && groupEnd + 1 < evt.ChainFlags.Length) &&
                                evt.ChainFlags[groupEnd + 1];

                            if (!gNextIsChain) break;
                            groupEnd++;
                        }

                        if (groupStart < groupEnd)
                        {
                            //체인 그룹 안에 있는 PARTS 스킬들을 미리 실행
                            for (int k = groupStart; k <= groupEnd; k++)
                            {
                                SkillEnum sid = evt.SkillIds[k];

                                //이미 선적용한 버프라면 다시 처리하지 않음
                                if (preAppliedParts.Contains(sid)) continue;

                                //PARTS 타입이 아니면 무시
                                if (!IsSkillType(sid, SkillType.PARTS)) continue;

                                //버프는 보통 자기 자신에게 적용
                                Transform t = _player != null ? _player.transform : transform;

                                //해당 버프 스킬이 체인으로 실행되는지
                                bool cflag = (evt.ChainFlags != null && k < evt.ChainFlags.Length) && evt.ChainFlags[k];

                                _player?.IdleAnimRoute();

                                //실제 전투 연출이 아니라, 버프 로직만 실행시키는 용도
                                bool ok = ExecuteSkillDirect(sid, cflag, t);

                                //선적용 성공 시 다시 실행하지 않게하고 쿨처리
                                if (ok)
                                {
                                    preAppliedParts.Add(sid);
                                    _executedThisTurn.Add(sid);
                                }
                            }
                        }
                    }

                    //현재 처리 중인 스킬이 PARTS인지 확인
                    bool curIsParts = IsSkillType(id, SkillType.PARTS);

                    //이미 선적용된 PARTS라면 스킵
                    if (curIsParts && preAppliedParts.Contains(id))
                    {
                        Bus<OnSkillExecutionStepEndEvent>.Raise(new OnSkillExecutionStepEndEvent(i));
                        yield return null;
                        continue;
                    }

                    bool shouldFlush = IsSkillType(id, SkillType.ATTACK);

                    BattleEnemy curTarget = _selector.GetCurrentTarget();
                    bool hasValidTarget = (curTarget != null && !curTarget.IsDead && curTarget.transform != null);

                    //타겟 확인
                    if (!hasValidTarget)
                    {
                        if (shouldFlush && TryReportBlockedAttackTarget())
                        {
                            Bus<OnSkillExecutionStepEndEvent>.Raise(new OnSkillExecutionStepEndEvent(i));
                            yield return new WaitForSeconds(0.2f);

                            if (!isChain) break;
                            continue;
                        }
                        
                        //타겟 없으면 
                        if (!isChain) break; 

                        Transform fallbackTarget = _player != null ? _player.transform : transform;

                        _player?.IdleAnimRoute();

                        bool executedNoTarget = ExecuteSkillDirect(id, true, fallbackTarget);

                        if (executedNoTarget)
                        {
                            _executedThisTurn.Add(id);

                            if (shouldFlush)
                            {
                                yield return new WaitForSeconds(0.1f);
                                _skillExecutor.FlushDeferredDamage();
                            }
                        }

                        Bus<OnSkillExecutionStepEndEvent>.Raise(new OnSkillExecutionStepEndEvent(i));
                        yield return null;
                        continue;
                    }
                    
                    if (shouldFlush && TryReportBlockedAttackTarget(curTarget))
                    {
                        Bus<OnSkillExecutionStepEndEvent>.Raise(new OnSkillExecutionStepEndEvent(i));
                        yield return new WaitForSeconds(0.2f);

                        if (!isChain) break;
                        continue;
                    }

                    //타겟 있을 때
                    Transform targetTr = curTarget.transform;
                    TryGetSkillData(id, out SkillDataSO skillData);

                    // 피드백이 없는 경우의 기본 처리
                    if (!isDashed)
                    {
                        yield return DashForwardRoutine();
                        isDashed = true;
                    }

                    yield return AttackRoutine(id, isChain, skillData, targetTr);
                    _skillExecutor.FlushDeferredDamage();

                    Bus<OnSkillExecutionStepEndEvent>.Raise(new OnSkillExecutionStepEndEvent(i));

                    yield return new WaitForSeconds(0.2f); 
                } // 스킬 루프 끝

                if (isDashed)
                {
                    yield return new WaitForSeconds(0.4f);
                    yield return _player.ReturnTo(startPos).WaitForCompletion();
                }
            }
            finally
            {
                Debug.Log("스킬 동작 끝!");
                _skillExecutor.FlushDeferredDamage();

                if (strongImpactFeedbackPlayer != null)
                    strongImpactFeedbackPlayer.ResetAllChainingAttack();

                if (_used != null)
                    skillsCache.SetActive(_used, false);

                RegisterCooldownsForExecutedSkills();

                EndTurnSafely();
            }
        }

        //대시 및 공격 처리 분리
        private IEnumerator DashForwardRoutine()
        {
            float hitOffsetX = 0.6f;
            Vector3 dashPos = new Vector3(
                _player.transform.position.x + hitOffsetX,
                _player.transform.position.y,
                _player.transform.position.z
            );

            //목적지로 대시할 때까지 대기
            yield return _player.AnticipateAndDashTo(dashPos).WaitForCompletion();
        }

        private IEnumerator AttackRoutine(SkillEnum id, bool isChain, SkillDataSO skillData, Transform targetTr)
        {
            bool isHit = false;
            bool isAnimEnd = false;

            Action onHit = () => isHit = true;
            Action onEnd = () => isAnimEnd = true;

            try
            {
                if (_animTrigger != null)
                {
                    _animTrigger.OnAttackTrigger += onHit;
                    _animTrigger.OnAnimationEndTrigger += onEnd;
                }

                _player.AttackAnimRoute();

                if (_animTrigger != null)
                    yield return WaitUntilOrTimeout(() => isHit, playerAttackHitTimeout, "플레이어 공격 타격 이벤트");
                else
                {
                    Debug.LogError("트리거 없음");
                    yield return new WaitForSeconds(0.3f);
                }

                if (strongImpactFeedbackPlayer != null && skillData != null)
                    strongImpactFeedbackPlayer.PlayIfNeeded(skillData, targetTr, _player.transform);

                if (ExecuteSkillDirect(id, isChain, targetTr))
                    _executedThisTurn.Add(id);

                if (_animTrigger != null)
                    yield return WaitUntilOrTimeout(() => isAnimEnd, playerAttackEndTimeout, "플레이어 공격 종료 이벤트");
                else
                {
                    Debug.LogError("트리거 없음");
                    yield return new WaitForSeconds(0.5f);
                }
            }
            finally
            {
                if (_animTrigger != null)
                {
                    _animTrigger.OnAttackTrigger -= onHit;
                    _animTrigger.OnAnimationEndTrigger -= onEnd;
                }
            }

            //_player.AttackAnimRoute();

            //yield return null;
        }

        private IEnumerator WaitUntilOrTimeout(Func<bool> predicate, float timeout, string label)
        {
            //애니메이션, 피드백 콜백이 누락돼도 스킬 코루틴이 끝까지 가도록 공통 대기
            float elapsed = 0f;

            while (predicate == null || !predicate())
            {
                if (timeout > 0f)
                {
                    elapsed += Time.deltaTime;
                    if (elapsed >= timeout)
                    {
                        Debug.LogWarning($"[플레이어 전투] {label} 대기 시간이 초과되어 스킬 흐름을 계속 진행합니다.", this);
                        yield break;
                    }
                }

                yield return null;
            }
        }

        public bool TryGetSkillData(SkillEnum id, out SkillDataSO skillData)
        {
            skillData = null;
            return skillsCache != null &&
                   skillsCache.TryGetSkillData(id, out skillData) &&
                   skillData != null;
        }

        private bool IsSkillType(SkillEnum id, SkillType skillType)
        {
            return TryGetSkillData(id, out SkillDataSO skillData) &&
                   skillData.skillType == skillType;
        }

        private bool ExecuteSkillDirect(SkillEnum id, bool isChain, Transform target)
        {
            if (_skillExecutor == null || target == null)
                return false;

            if (_cooldowns.TryGetValue(id, out int remainingTurn) && remainingTurn > 0)
                return false;

            if (!_skillExecutor.CanExecuteById(id, target))
                return false;

            return _skillExecutor.ExecuteById(id, isChain, target);
        }

        private void RegisterCooldownsForExecutedSkills()
        {
            foreach (SkillEnum id in _executedThisTurn)
            {
                if (!TryGetSkillData(id, out SkillDataSO skillData))
                    continue;

                int cooldownTurn = skillData.cooldownTurn;
                if (cooldownTurn > 0)
                    _cooldowns[id] = cooldownTurn;
            }
        }

        private void OnVoidEndAction(OnVoidEndActionEvent evt)
        {
            Debug.Log("스킬 동작 끝");
            _skillExecutor.FlushDeferredDamage();

            if (strongImpactFeedbackPlayer != null)
                strongImpactFeedbackPlayer.ResetAllChainingAttack();

            if (_used != null)
                skillsCache.SetActive(_used, false);

            RegisterCooldownsForExecutedSkills();

            EndTurnSafely();
        }

        private void EndTurnSafely()
        {
            if (_turnEnded) return;
            _turnEnded = true;
            
            if (_skillExecutor != null)
            {
                _skillExecutor.Dispose();
                _skillExecutor = null;
            }

            _attackCo = null;

            Bus<OnSkillExecutionEndEvent>.Raise(new OnSkillExecutionEndEvent());

            if (_allyManager != null && _allyManager.TryStartAllyAttackSequence(AdvanceToEnemyTurn))
                return;

            AdvanceToEnemyTurn();
        }

        private void AdvanceToEnemyTurn()
        {
            _player?.TurnManager?.NextTurn();
        }

        public bool RequestEndTurnFromExternal()
        {
            if (!isActiveAndEnabled) return false;
            if (_player == null) return false;
            if (_turnEnded) return true;
            if (_attackCo != null) return true;

            EndTurnSafely();
            return true;
        }
        
        private bool TryReportBlockedAttackTarget()
        {
            if (_enemyManager == null)
                return false;

            var enemies = _enemyManager.GetEnemies();
            if (enemies == null || enemies.Count == 0)
                return false;

            int selectedIndex = _selector != null ? _selector.GetCurrentTargetIndex() : -1;

            if (selectedIndex >= 0 && selectedIndex < enemies.Count)
            {
                if (TryReportBlockedAttackTarget(enemies[selectedIndex]))
                    return true;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                if (i == selectedIndex)
                    continue;

                if (TryReportBlockedAttackTarget(enemies[i]))
                    return true;
            }

            return false;
        }

        private bool TryReportBlockedAttackTarget(Entity target)
        {
            if (target == null || target.IsDead)
                return false;

            if (!SkillTargetingUtil.CanBeRangeTarget(target, true))
                return true;

            if (!SkillTargetingUtil.CanBeDirectTarget(target, false))
                return true;

            return false;
        }
        
    }
}
