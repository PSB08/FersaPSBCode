using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using PSB.Code.BattleCode.Allies.AttackCode;
using PSB.Code.BattleCode.Players;
using PSB_Lib.Dependencies;
using PSB.Code.BattleCode.Allies.BTs.Events;
using PSW.Code.EventBus;
using UnityEngine;
using Work.PSB.Code.RunSystem;

namespace PSB.Code.BattleCode.Allies
{
    [DefaultExecutionOrder(-5)]
    [Provide]
    public class BattleAllyManager : MonoBehaviour, IDependencyProvider
    {
        [SerializeField] private List<BattleAlly> allies = new();
        [SerializeField, Min(0f)] private float attackDelay = 0.5f;
        [SerializeField, Min(0f)] private float allyTurnDoneTimeout = 8f;
        [SerializeField, Min(0f)] private float targetReadyTimeout = 1.2f;

        [Header("배치 설정")]
        [SerializeField] private Vector2 upperOffset = new(-1.1f, 1.35f);
        [SerializeField] private Vector2 lowerOffset = new(-1.1f, -1.35f);
        [SerializeField] private float arrangeDuration = 0.5f;
        [SerializeField] private Ease arrangeEase = Ease.OutCubic;

        [Inject] private PlayerManager _playerManager;

        private readonly Dictionary<BattleAlly, Tween> _arrangeTweens = new();
        private Coroutine _seqCo;
        private bool _isAttackSequenceRunning;
        private Action _onSequenceComplete;
        private BattleAlly _waitingBattleAlly;
        private bool _waitingDone;
        private float _turnDoneElapsed;
        private bool _initialArrangeDone;
        private bool _arrangeDirty;

        private object ArrangeTweenId(BattleAlly ally) => (ally, "ALLY_ARRANGE");
        public float ArrangeDuration => arrangeDuration;

        private void Awake()
        {
            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);
        }

        private void OnEnable() 
        {
            Bus<AllyTurnDoneEvent>.OnEvent += OnAllyTurnDone;
        }

        private IEnumerator Start()
        {
            _initialArrangeDone = false;
            _arrangeDirty = false;

            for (int i = allies.Count - 1; i >= 0; i--)
            {
                BattleAlly ally = allies[i];
                if (ally == null || !ally.gameObject.activeInHierarchy || ally.IsDead)
                {
                    allies.RemoveAt(i);
                    continue;
                }

                RegisterPartyTarget(ally);
            }

            SetAlliesVisible(false);
            RefreshIndicesAndArrange(false);

            yield return null;

            SetAlliesVisible(true);
            _initialArrangeDone = true;
        }

        private void OnDisable()
        {
            Bus<AllyTurnDoneEvent>.OnEvent -= OnAllyTurnDone;

            if (_seqCo != null)
            {
                StopCoroutine(_seqCo);
                _seqCo = null;
            }

            _isAttackSequenceRunning = false;
            _onSequenceComplete = null;
            _waitingBattleAlly = null;
            _waitingDone = false;
            _arrangeDirty = false;
            KillAllArrangeTweens();
        }

        public bool Register(BattleAlly ally)
        {
            if (ally == null)
                return false;

            if (allies.Contains(ally))
            {
                RegisterPartyTarget(ally);
                ArrangeAfterRegister(ally);
                return true;
            }

            if (allies.Count >= 2)
            {
                Debug.LogWarning("[BattleAllyManager] 동료는 최대 2명까지만 전투에 참여할 수 있습니다.", this);
                return false;
            }

            allies.Add(ally);
            RegisterPartyTarget(ally);
            ArrangeAfterRegister(ally);
            return true;
        }

        public bool Register(BattleAlly ally, int preferredIndex)
        {
            if (preferredIndex < 0)
                return Register(ally);

            if (ally == null)
                return false;

            bool alreadyRegistered = allies.Remove(ally);
            if (!alreadyRegistered && allies.Count >= 2)
            {
                Debug.LogWarning("[BattleAllyManager] Ally battle slots are full.", this);
                return false;
            }

            int insertIndex = Mathf.Clamp(preferredIndex, 0, allies.Count);
            allies.Insert(insertIndex, ally);

            RegisterPartyTarget(ally);
            ArrangeAfterRegister(ally);
            return true;
        }

        public void Unregister(BattleAlly ally)
        {
            if (ally == null)
                return;

            if (DOTween.instance != null)
                DOTween.Kill(ArrangeTweenId(ally));

            _arrangeTweens.Remove(ally);

            bool removed = allies.Remove(ally);
            UnregisterPartyTarget(ally);

            if (removed)
                MarkArrangeDirty();
        }

        public IReadOnlyList<BattleAlly> GetAllies() => allies;

        public Vector3 GetNextAllySlotPosition()
        {
            int activeCount = 0;
            for (int i = 0; i < allies.Count; i++)
            {
                BattleAlly ally = allies[i];
                if (ally != null && ally.gameObject.activeInHierarchy && !ally.IsDead)
                    activeCount++;
            }

            int index = Mathf.Clamp(activeCount, 0, 1);
            return GetSlotPosition(index);
        }

        public Vector3 GetAllySlotPosition(int index)
        {
            return GetSlotPosition(Mathf.Clamp(index, 0, 1));
        }

        public bool PlayEntryFromOffset(BattleAlly ally, float entryXOffset)
        {
            if (ally == null)
                return false;

            int index = allies.IndexOf(ally);
            if (index < 0)
                return false;

            Vector3 target = GetSlotPosition(index);
            Vector3 entryPosition = new(target.x + entryXOffset, target.y, target.z);

            if (DOTween.instance != null)
                DOTween.Kill(ArrangeTweenId(ally));

            ally.transform.position = entryPosition;
            MoveAllyToSlot(ally, index, true);
            return true;
        }

        public bool TryStartAllyAttackSequence(Action onComplete)
        {
            if (_isAttackSequenceRunning)
            {
                _onSequenceComplete += onComplete;
                return true;
            }
            
            if (RunEventBattleHealthController.ShouldStopAdditionalActions)
                return false;

            if (!HasAnyActiveAlly())
                return false;

            _onSequenceComplete = onComplete;
            _seqCo = StartCoroutine(AllyAttackSequence());
            return true;
        }

        private IEnumerator AllyAttackSequence()
        {
            _isAttackSequenceRunning = true;

            if (_arrangeDirty)
            {
                yield return new WaitForSeconds(attackDelay);
            }
            else
            {
                RefreshIndicesAndArrange(true);
                yield return new WaitForSeconds(arrangeDuration + attackDelay);
            }

            List<BattleAlly> activeAllies = new(allies);
            for (int i = 0; i < activeAllies.Count; i++)
            {
                BattleAlly ally = activeAllies[i];
                if (ally == null || !ally.gameObject.activeInHierarchy || ally.IsDead || ally.IsFreeze)
                    continue;

                AllyAttack attack = ally.GetModule<AllyAttack>();
                if (attack == null || !attack.HasAttackSkill)
                    continue;

                //ally.buffModule?.UpdateTime();

                yield return WaitForAttackTargetReady(ally, attack);

                if (!CanStartAllyAttack(ally, attack))
                    continue;

                _waitingBattleAlly = ally;
                _waitingDone = false;
                _turnDoneElapsed = 0f;

                if (!ally.TrySendBTState(BattleAllyState.Move))
                {
                    _waitingBattleAlly = null;
                    _waitingDone = false;
                    continue;
                }

                yield return WaitForAllyTurnDone(ally);

                _waitingBattleAlly = null;
                
                if (RunEventBattleHealthController.ShouldStopAdditionalActions)
                    break;

                if (ally != null && !ally.IsDead)
                    yield return new WaitForSeconds(attackDelay);
            }

            _isAttackSequenceRunning = false;
            _seqCo = null;
            _waitingBattleAlly = null;
            _waitingDone = false;

            Action callback = _onSequenceComplete;
            _onSequenceComplete = null;
            callback?.Invoke();
        }

        private IEnumerator WaitForAttackTargetReady(BattleAlly ally, AllyAttack attack)
        {
            if (targetReadyTimeout <= 0f)
                yield break;

            float elapsed = 0f;
            while (ally != null && ally.gameObject.activeInHierarchy &&
                   !ally.IsDead && !ally.IsFreeze && attack != null &&
                   !attack.TrySelectTargetTransform(out _, out _) && elapsed < targetReadyTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private bool CanStartAllyAttack(BattleAlly ally, AllyAttack attack)
        {
            if (ally == null || attack == null)
                return false;

            if (ally.IsDead || !ally.gameObject.activeInHierarchy || ally.IsFreeze)
                return false;

            if (attack.TrySelectTargetTransform(out _, out string reason))
                return true;

            attack.LogAiDebug($"공격 가능한 대상 없음 : {reason}. 동료 행동을 즉시 넘깁니다.");
            attack.EndAllyTurnPlanning();
            
            ally.SendBTState(BattleAllyState.Idle);
            return false;
        }

        private void OnAllyTurnDone(AllyTurnDoneEvent evt)
        {
            if (!_isAttackSequenceRunning || _waitingBattleAlly == null || evt.BattleAlly == null)
                return;

            if (ReferenceEquals(evt.BattleAlly, _waitingBattleAlly))
                _waitingDone = true;
        }

        private IEnumerator WaitForAllyTurnDone(BattleAlly ally)
        {
            while (!_waitingDone && !IsWaitingAllyGone())
            {
                if (allyTurnDoneTimeout > 0f)
                {
                    _turnDoneElapsed += Time.deltaTime;
                    if (_turnDoneElapsed >= allyTurnDoneTimeout)
                    {
                        Debug.LogWarning($"[BattleAllyManager] {ally?.name} ally turn done timeout. " +
                                         $"Force continuing ally sequence.", ally);

                        ally?.SendBTState(BattleAllyState.Idle);
                        _waitingDone = true;
                        yield break;
                    }
                }

                yield return null;
            }
        }

        public void ResetTurnDoneTimeout(BattleAlly ally)
        {
            if (ally != null && ReferenceEquals(ally, _waitingBattleAlly))
                _turnDoneElapsed = 0f;
        }

        private bool IsWaitingAllyGone()
            => _waitingBattleAlly == null || !_waitingBattleAlly.gameObject.activeInHierarchy 
                                          || _waitingBattleAlly.IsDead;

        private bool HasAnyActiveAlly()
        {
            for (int i = 0; i < allies.Count; i++)
            {
                BattleAlly ally = allies[i];
                if (ally != null && ally.gameObject.activeInHierarchy && !ally.IsDead)
                    return true;
            }

            return false;
        }

        private void RegisterPartyTarget(BattleAlly ally)
        {
            ResolvePlayerManager()?.RegisterPartyTarget(ally);
        }

        private void UnregisterPartyTarget(BattleAlly ally)
        {
            ResolvePlayerManager()?.UnregisterPartyTarget(ally);
        }

        private PlayerManager ResolvePlayerManager()
        {
            if (_playerManager == null)
                _playerManager = FindAnyObjectByType<PlayerManager>();

            return _playerManager;
        }

        public void ArrangeDirtyAlliesForPlayerTurn()
        {
            if (!_initialArrangeDone || !_arrangeDirty)
                return;

            _arrangeDirty = false;
            RefreshIndicesAndArrange(true);
        }

        private void ArrangeAfterRegister(BattleAlly ally)
        {
            if (!_initialArrangeDone)
            {
                SetAllyVisible(ally, false);
                RefreshIndicesAndArrange(false);
                return;
            }

            RefreshIndicesAndArrange(true);
        }

        private void MarkArrangeDirty()
        {
            if (!_initialArrangeDone)
            {
                RefreshIndicesAndArrange(false);
                return;
            }

            _arrangeDirty = true;
        }

        private void RefreshIndicesAndArrange(bool tween)
        {
            allies.RemoveAll(ally => ally == null);

            for (int i = 0; i < allies.Count; i++)
            {
                BattleAlly ally = allies[i];
                if (ally == null) continue;
                
                ally.SetAllyIndex(i);
                MoveAllyToSlot(ally, i, tween);
            }
        }

        private void MoveAllyToSlot(BattleAlly ally, int index, bool tween)
        {
            if (ally == null)
                return;

            Vector3 target = GetSlotPosition(index);

            if (!tween)
            {
                ally.transform.position = target;
                return;
            }

            if (DOTween.instance != null)
                DOTween.Kill(ArrangeTweenId(ally));

            _arrangeTweens[ally] = ally.transform.DOMove(target, arrangeDuration)
                .SetEase(arrangeEase).SetId(ArrangeTweenId(ally));
        }

        private Vector3 GetSlotPosition(int index)
        {
            PlayerManager playerManager = ResolvePlayerManager();
            Vector3 center = playerManager != null && playerManager.BattlePlayer != null 
                ? playerManager.BattlePlayer.transform.position : transform.position;

            Vector2 offset = index == 0 ? upperOffset : lowerOffset;
            return new Vector3(center.x + offset.x, center.y + offset.y, center.z);
        }

        private void SetAlliesVisible(bool visible)
        {
            for (int i = 0; i < allies.Count; i++)
                SetAllyVisible(allies[i], visible);
        }

        private void SetAllyVisible(BattleAlly ally, bool visible)
        {
            if (ally == null)
                return;

            ally.SetRenderersVisible(visible);
        }

        private void KillAllArrangeTweens()
        {
            foreach (KeyValuePair<BattleAlly, Tween> kv in _arrangeTweens)
            {
                if (kv.Value != null && kv.Value.IsActive())
                    kv.Value.Kill();
            }

            _arrangeTweens.Clear();
        }
        
        public void UpdateAllAllyBuffs()
        {
            List<BattleAlly> snapshot = new(allies);

            foreach (BattleAlly ally in snapshot)
            {
                if (ally == null) continue;
                if (!ally.gameObject.activeInHierarchy) continue;
                if (ally.IsDead) continue;
                if (!allies.Contains(ally)) continue;

                ally.buffModule?.UpdateTime();
            }
        }
        
    }
}
