using System.Collections;
using System.Collections.Generic;
using CIW.Code.System.Events;
using DG.Tweening;
using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSB_Lib.StatSystem;
using PSB.Code.BattleCode.Commands;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Modules;
using PSB.Code.BattleCode.Enemies;
using Work.YIS.Code.Skills;
using YIS.Code.CoreSystem;
using YIS.Code.Defines;
using YIS.Code.Skills;
using CIW.Code.Feedbacks;
using YIS.Code.UI;
using YIS.Code.Events;
using CIW.Code.Entities;
using System;

namespace PSB.Code.BattleCode.Players
{
    public class PlayerCombatController : MonoBehaviour, IModule
    {
        [Header("Refs")]
        [SerializeField] private SkillCommandSO skillCommand;
        [SerializeField] private StatSO procChanceStat;
        [SerializeField] private PlayerSkillsCache skillsCache;

        private BattlePlayer _player;
        private PlayerTargetSelector _selector;
        private PlayerSkillExecutor _skillExecutor;
        EntityAnimatorTrigger _animTrigger;

        private Coroutine _attackCo;
        private bool _turnEnded;
        private SkillEnum[] _used;

        [Inject] private PoolManagerMono _poolManager;
        [Inject] private BattleEnemyManager _enemyManager;
        [Inject] private BattleActionQueue _battleActionQueue;

        private readonly HashSet<SkillEnum> _executedThisTurn = new HashSet<SkillEnum>();
        private readonly HashSet<SkillEnum> _cooldownRunning = new HashSet<SkillEnum>();

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

            if (_attackCo != null)
            {
                StopCoroutine(_attackCo);
                _attackCo = null;
            }
        }

        public void OnPlayerTurnStarted()
        {
            if (skillsCache == null) return;
            if (_cooldownRunning.Count == 0) return;

            var stillRunning = new HashSet<SkillEnum>();

            foreach (var id in _cooldownRunning)
            {
                if (!skillsCache.TryGetOrCreate(id, out var skill) || skill == null)
                    continue;

                skill.TickCooldown();

                if (skill.CurrentCooldown > 0)
                    stillRunning.Add(id);
            }

            _cooldownRunning.Clear();
            foreach (var id in stillRunning)
                _cooldownRunning.Add(id);
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
            if (skillCommand == null || skillsCache == null || _poolManager == null || _enemyManager == null || _player.IsFreeze)
            {
                EndTurnSafely();
                yield break;
            }
            
            if (_skillExecutor != null)
            {
                _skillExecutor.Dispose();
            }

            //이번 턴에 사용할 SkillExecutor 생성
            _skillExecutor = new PlayerSkillExecutor(
                skillsCache, _poolManager, _enemyManager, 
                _selector, _battleActionQueue, _player, procChanceStat, deferDamage: false
            );

            //이미 선적용한 PARTS 스킬을 기록해서 같은 버프가 두 번 실행되는 방지하기 위한 집합
            var preAppliedParts = new HashSet<SkillEnum>();

            _used = evt.SkillIds;
            Vector3 startPos = _player.transform.position;
            
            bool isDashed = false;
            bool isZoomed = false;
            FeedbackSkill lastFeedbackSkill = null;

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

                                //스킬 조회 실패 시 스킵
                                if (!_skillExecutor.TryGetSkill(sid, out BaseSkill ps) || ps == null) continue;
                                if (ps.SkillData == null) continue;

                                //PARTS 타입이 아니면 무시
                                if (ps.SkillData.skillType != SkillType.PARTS) continue;

                                //버프는 보통 자기 자신에게 적용
                                Transform t = _player != null ? _player.transform : transform;

                                //해당 버프 스킬이 체인으로 실행되는지
                                bool cflag = (evt.ChainFlags != null && k < evt.ChainFlags.Length) && evt.ChainFlags[k];

                                _player?.IdleAnimRoute();

                                //실제 전투 연출이 아니라, 버프 로직만 실행시키는 용도
                                Context preCtx = new Context(cflag, new[] { sid }, _player, t, _skillExecutor, new[] { cflag });

                                bool ok = false;
                                if (skillCommand.CanHandle(preCtx))
                                    ok = skillCommand.Handle(preCtx);

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
                    bool curIsParts = false;
                    if (_skillExecutor.TryGetSkill(id, out BaseSkill curSkill) && curSkill != null && curSkill.SkillData != null)
                    {
                        curIsParts = curSkill.SkillData.skillType == SkillType.PARTS;
                    }

                    //이미 선적용된 PARTS라면 스킵
                    if (curIsParts && preAppliedParts.Contains(id))
                    {
                        yield return null;
                        continue;
                    }

                    bool shouldFlush = false;
                    if (_skillExecutor.TryGetSkill(id, out BaseSkill ss) && ss != null && ss.SkillData != null)
                    {
                        shouldFlush = ss.SkillData.skillType == SkillType.ATTACK;
                    }

                    BattleEnemy curTarget = _selector.GetCurrentTarget();
                    bool hasValidTarget = (curTarget != null && !curTarget.IsDead && curTarget.transform != null);

                    //타겟 확인
                    if (!hasValidTarget)
                    {
                        //타겟 없으면 
                        if (!isChain) break; 

                        Transform fallbackTarget = _player != null ? _player.transform : transform;

                        bool skipVisual = false;
                        if (_skillExecutor.TryGetSkill(id, out BaseSkill skill) && skill != null)
                            skipVisual = skill.SkipAnim(true);

                        Context ctxNoTarget = new Context(true, new[] { id }, _player, fallbackTarget, _skillExecutor, new[] { true });

                        _player?.IdleAnimRoute();

                        bool executedNoTarget = false;
                        if (skillCommand.CanHandle(ctxNoTarget))
                            executedNoTarget = skillCommand.Handle(ctxNoTarget);

                        if (executedNoTarget)
                        {
                            _executedThisTurn.Add(id);

                            if (shouldFlush)
                            {
                                yield return new WaitForSeconds(0.1f);
                                _skillExecutor.FlushDeferredDamage();
                            }
                        }

                        yield return null;
                        continue;
                    }

                    //타겟 있을 때
                    Transform targetTr = curTarget.transform;

                    bool skipVisual2 = false;
                    _skillExecutor.TryGetSkill(id, out BaseSkill skillInstance);
                    if (skillInstance != null) skipVisual2 = skillInstance.SkipAnim(isChain);

                    Context ctx = new Context(isChain, new[] { id }, _player, targetTr, _skillExecutor, new[] { isChain });

                    if (skipVisual2)
                    {
                        if (_player != null) _player.IdleAnimRoute();

                        bool executed = false;
                        if (skillCommand.CanHandle(ctx))
                            executed = skillCommand.Handle(ctx);

                        if (executed)
                        {
                            _executedThisTurn.Add(id);

                            if (shouldFlush)
                            {
                                yield return new WaitForSeconds(0.1f);
                                _skillExecutor.FlushDeferredDamage();
                            }
                        }

                        yield return null;
                        continue;
                    }

                    if (skillInstance != null && skillInstance.TryGetComponent<FeedbackSkill>(out var feedbackSkill))
                    {
                        lastFeedbackSkill = feedbackSkill;

                        bool canDash = false;
                        bool isAllDone = false;

                        bool doZoomIn = !isZoomed;
                        
                        // 루프 안에서는 줌아웃을 절대 실행하지 않음
                        feedbackSkill.Execute(isChain, targetTr, doZoomIn, false,
                            onZoomComplete: () => canDash = true,  
                            onHit: () => { _skillExecutor.FlushDeferredDamage(1); }, 
                            onShrinkComplete: null, 
                            onComplete: () => isAllDone = true);

                        if (doZoomIn)
                        {
                            yield return new WaitUntil(() => canDash);
                            isZoomed = true;
                        }

                        if (!isDashed)
                        {
                            yield return DashForwardRoutine();
                            isDashed = true;
                        }

                        yield return AttackRoutine(id, ctx);

                        yield return new WaitUntil(() => isAllDone);
                    }
                    else
                    {
                        // 피드백이 없는 경우의 기본 처리
                        if (!isDashed)
                        {
                            yield return DashForwardRoutine();
                            isDashed = true;
                        }

                        yield return AttackRoutine(id, ctx);
                        _skillExecutor.FlushDeferredDamage();
                    }

                    yield return new WaitForSeconds(0.2f); 
                } // 스킬 루프 끝

                if (isZoomed && lastFeedbackSkill != null)
                {
                    bool isShrinkDone = false;
                    lastFeedbackSkill.ForceZoomOut(() => isShrinkDone = true);
                    yield return new WaitUntil(() => isShrinkDone);
                }

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

                if (_used != null)
                    skillsCache.SetActive(_used, false);

                foreach (var id in _executedThisTurn)
                {
                    if (skillsCache.TryGetOrCreate(id, out var s) && s != null && s.CurrentCooldown > 0)
                        _cooldownRunning.Add(id);
                }

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

        private IEnumerator AttackRoutine(SkillEnum id, Context ctx)
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
                    yield return new WaitUntil(() => isHit);
                else
                {
                    Debug.LogError("트리거 없음");
                    yield return new WaitForSeconds(0.3f);
                }

                if (skillCommand.CanHandle(ctx))
                {
                    if (skillCommand.Handle(ctx))
                        _executedThisTurn.Add(id);
                }

                if (_animTrigger != null)
                    yield return new WaitUntil(() => isAnimEnd);
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

            //if (skillCommand.CanHandle(ctx))
            //{
            //    if (skillCommand.Handle(ctx)) 
            //        _executedThisTurn.Add(id);
            //}
            //yield return null;
        }

        private void OnVoidEndAction(OnVoidEndActionEvent evt)
        {
            Debug.Log("스킬 동작 끝");
            _skillExecutor.FlushDeferredDamage();

            if (_used != null)
                skillsCache.SetActive(_used, false);

            foreach (var id in _executedThisTurn)
            {
                if (skillsCache.TryGetOrCreate(id, out var s) && s != null && s.CurrentCooldown > 0)
                    _cooldownRunning.Add(id);
            }

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
            _player?.TurnManager?.NextTurn();
        }
        
    }
}