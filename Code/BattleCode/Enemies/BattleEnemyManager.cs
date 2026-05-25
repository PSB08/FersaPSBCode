using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using PSB.Code.CoreSystem.Events;
using PSB_Lib.Dependencies;
using PSB.Code.BattleCode.Enemies.BTs.Events;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using Work.CSH.Scripts.Managers;
using Work.PSB.Code.FieldCode;

namespace PSB.Code.BattleCode.Enemies
{
    [Serializable]
    public struct BatchSize
    {
        public Vector2 startSize;
        public Vector2 endSize;
    }

    [DefaultExecutionOrder(-5)]
    [Provide]
    public class BattleEnemyManager : MonoBehaviour, IDependencyProvider
    {
        [SerializeField] private List<BattleEnemy> enemies = new();
        [SerializeField] private float attackDelay = 0.8f;

        [Header("배치 범위 설정")]
        [SerializeField] private BatchSize batchSize;

        [Header("배치 설정")]
        [SerializeField] private Vector2 startPosition = new(0f, 0f);
        [SerializeField] private Vector2 cellSize = new(1.5f, 1.5f);
        [SerializeField] private int columns = 5;

        [Header("정렬 설정")]
        [SerializeField] private float arrangeDuration = 0.35f;
        [SerializeField] private Ease arrangeEase = Ease.OutCubic;
        [SerializeField] private bool killPreviousArrangeTween = true;
        [SerializeField] private float arrangeStagger = 0.03f;
        [SerializeField] private Vector2 spawnOffset = new Vector2(25f, 0f);

        private Dictionary<BattleEnemy, Tween> _arrangeTweens = new();
        private object ArrangeTweenId(BattleEnemy e) => (e, "ARRANGE");

        private bool _initialArrangeDone;
        private bool _battleEndRaised;
        private bool _arrangeDirty;
        private bool _isFirstArrange = true;
        
        private List<BattleEnemy> _arrangedEnemies = new List<BattleEnemy>();
        private Queue<BattleEnemy> _waitingQueue = new Queue<BattleEnemy>();
        
        private int _pendingSpawnCount = 0;

        private bool _isAttackSequenceRunning;
        private TurnManagerSO _turnManagerCache;

        private BattleEnemy _waitingBattleEnemy;
        private bool _waitingDone;
        private Coroutine _seqCo;

        private void OnEnable()
        {
            Bus<EnemyTurnDoneEvent>.OnEvent += OnEnemyTurnDone;
            Bus<PhaseStartEvent>.OnEvent += OnPhaseStart;
            
            ResetState();
            _isFirstArrange = true;
        }

        private void OnDisable()
        {
            Bus<EnemyTurnDoneEvent>.OnEvent -= OnEnemyTurnDone;
            Bus<PhaseStartEvent>.OnEvent -= OnPhaseStart;
            
            StopSequenceIfRunning();
            KillAllArrangeTweens();
            if (_turnManagerCache != null)
            {
                _turnManagerCache.OnBattleEndedCondition = null;
                _turnManagerCache.OnTurnStarted -= HandleTurnStarted;
            }
        }

        private void ResetState()
        {
            StopSequenceIfRunning();
            KillAllArrangeTweens();
            
            _initialArrangeDone = false;
            _battleEndRaised = false;
            _arrangeDirty = false;
            
            _arrangedEnemies.Clear();
            _waitingQueue.Clear();
            _pendingSpawnCount = 0;
        }

        private void OnPhaseStart(PhaseStartEvent evt)
        {
            bool wasRunning = _isAttackSequenceRunning;
            ResetState();
            
            _isFirstArrange = (evt.PhaseIndex == 0);
            
            StartCoroutine(InitArrangeCoroutine());

            if (wasRunning)
            {
                TryStartEnemyAttackSequence();
            }
        }

        private void StopSequenceIfRunning()
        {
            if (_seqCo != null)
            {
                StopCoroutine(_seqCo);
                _seqCo = null;
            }

            _isAttackSequenceRunning = false;
            _waitingBattleEnemy = null;
            _waitingDone = false;
        }

        private IEnumerator InitArrangeCoroutine()
        {
            SetEnemiesVisible(false);

            if (_isFirstArrange)
            {
                EnemyArrangementStrategy.SortEnemiesList(enemies);
            }

            bool doTween = !_isFirstArrange;
            List<Vector3> targets = EnemyArrangementStrategy.ComputePositions
                (enemies, columns, startPosition, cellSize, batchSize);
            
            if (targets != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] == null) continue;
                    enemies[i].transform.position = doTween ? targets[i] + (Vector3)spawnOffset : targets[i];
                    _arrangedEnemies.Add(enemies[i]);
                }
            }

            yield return null;
            SetEnemiesVisible(true);

            if (targets != null && doTween)
            {
                MoveEnemiesToTargets(targets, true);
            }

            EnemyArrangementStrategy.ApplySortingOrders(enemies);

            _isFirstArrange = false;

            if (targets != null && doTween)
            {
                yield return new WaitForSeconds(arrangeDuration + (enemies.Count * arrangeStagger));
                _arrangeDirty = true;
            }

            _initialArrangeDone = true;
        }

        private void Update()
        {
            if (!_battleEndRaised && _initialArrangeDone && enemies.Count <= 0 && _waitingQueue.Count <= 0)
            {
                _battleEndRaised = true;
                Debug.Log("현재 페이즈 적 다 죽었다");
                Bus<PhaseClearEvent>.Raise(new PhaseClearEvent());
            }
        }

        public void Register(BattleEnemy battleEnemy)
        {
            if (battleEnemy == null || enemies.Contains(battleEnemy) 
                                    || _waitingQueue.Contains(battleEnemy)) return;

            battleEnemy.transform.position = (Vector3)startPosition + (Vector3)spawnOffset;
            
            SetEnemyVisible(battleEnemy, false);

            if (enemies.Count >= columns)
            {
                _waitingQueue.Enqueue(battleEnemy);
                return;
            }

            AddNewEnemyToField(battleEnemy);
        }

        public void Unregister(BattleEnemy battleEnemy)
        {
            if (battleEnemy == null) return;

            if (_waitingQueue.Contains(battleEnemy))
            {
                _waitingQueue = new Queue<BattleEnemy>(_waitingQueue.Where(x => x != battleEnemy));
                return;
            }

            int deadIndex = enemies.IndexOf(battleEnemy);
            if (deadIndex < 0) return;

            DOTween.Kill(ArrangeTweenId(battleEnemy));
            _arrangeTweens.Remove(battleEnemy);

            enemies.RemoveAt(deadIndex);
            _arrangedEnemies.Remove(battleEnemy); 
            
            if (_waitingBattleEnemy == battleEnemy)
            {
                _waitingBattleEnemy = null;
                _waitingDone = true;
            }
            
            if (_waitingQueue.Count > 0)
            {
                _pendingSpawnCount++;
                StartCoroutine(WaitDeathSpawnCoroutine(battleEnemy, deadIndex));
            }
            else if (!_initialArrangeDone)
            {
                ArrangeEnemies(false);
            }
            else
            {
                _arrangeDirty = true;
            }

            Bus<EnemyListChanged>.Raise(new EnemyListChanged());
        }
        
        private void AddNewEnemyToField(BattleEnemy battleEnemy, int insertIndex = -1)
        {
            if (insertIndex >= 0 && insertIndex <= enemies.Count)
            {
                enemies.Insert(insertIndex, battleEnemy);
            }
            else
            {
                enemies.Add(battleEnemy);
            }

            if (_initialArrangeDone)
            {
                _arrangeDirty = true; 
                
                List<Vector3> targets = EnemyArrangementStrategy.ComputePositions
                    (enemies, columns, startPosition, cellSize, batchSize);
                int idx = enemies.IndexOf(battleEnemy);
                
                if (targets != null && idx >= 0 && idx < targets.Count)
                {
                    battleEnemy.transform.position = targets[idx] + (Vector3)spawnOffset;
                    _arrangedEnemies.Add(battleEnemy);
                    MoveEnemyToTarget(battleEnemy, targets[idx], 0f);
                }
                
                SetEnemyVisible(battleEnemy, true);
            }

            CacheTurnManager(battleEnemy);
            Bus<EnemyListChanged>.Raise(new EnemyListChanged());
        }

        private IEnumerator WaitDeathSpawnCoroutine(BattleEnemy dyingEnemy, int insertIndex)
        {
            try
            {
                if (dyingEnemy != null)
                {
                    yield return new WaitUntil(() => dyingEnemy == null || 
                                                     !dyingEnemy.gameObject.activeInHierarchy);
                }

                if (_waitingQueue.Count > 0)
                {
                    BattleEnemy nextEnemy = _waitingQueue.Dequeue();
                    AddNewEnemyToField(nextEnemy, insertIndex);
                    
                    yield return new WaitForSeconds(arrangeDuration);
                }
            }
            finally
            {
                _pendingSpawnCount = Mathf.Max(0, _pendingSpawnCount - 1);
            }
        }

        private void SetEnemyVisible(BattleEnemy e, bool visible)
        {
            if (e == null) return;
            
            Renderer[] renderers = e.GetComponentsInChildren<Renderer>(true);
            
            for (int r = 0; r < renderers.Length; r++)
                renderers[r].enabled = visible;
        }

        private void SetEnemiesVisible(bool visible)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                SetEnemyVisible(enemies[i], visible);
            }
        }

        public IReadOnlyList<BattleEnemy> GetEnemies() => enemies;

        private void CacheTurnManager(BattleEnemy battleEnemy)
        {
            if (_turnManagerCache == null && battleEnemy != null && battleEnemy.TurnManager != null)
            {
                _turnManagerCache = battleEnemy.TurnManager;
                _turnManagerCache.OnBattleEndedCondition = CheckAllEnemiesDead;

                _turnManagerCache.OnTurnStarted += HandleTurnStarted;
            }
        }

        private bool CheckAllEnemiesDead()
        {
            if (_battleEndRaised) return CheckLastPhaseCondition();

            foreach (var e in enemies)
            {
                if (e != null && e.gameObject.activeInHierarchy && !e.IsDead)
                    return false;
            }

            if (_waitingQueue.Count > 0) return false;

            return CheckLastPhaseCondition();
        }

        private bool CheckLastPhaseCondition()
        {
            if (BattleRuntimeData.EncounterData != null)
            {
                return BattleRuntimeData.CurrentPhaseIndex >= BattleRuntimeData.EncounterData.phases.Length - 1;
            }
            return true;
        }

        private void ArrangeEnemies(bool tween)
        {
            List<Vector3> targets = EnemyArrangementStrategy.ComputePositions
                (enemies, columns, startPosition, cellSize, batchSize);
            if (targets == null) return;

            if (tween)
            {
                MoveEnemiesToTargets(targets, true);
            }
            else
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] == null) continue;
                    enemies[i].transform.position = targets[i];
                    _arrangedEnemies.Add(enemies[i]); 
                }
            }

            EnemyArrangementStrategy.ApplySortingOrders(enemies);
        }

        private void MoveEnemyToTarget(BattleEnemy enemy, Vector3 targetPos, float delay)
        {
            if (killPreviousArrangeTween) DOTween.Kill(ArrangeTweenId(enemy));
            enemy.transform.DOKill();

            _arrangeTweens[enemy] = enemy.transform
                .DOMove(targetPos, arrangeDuration)
                .SetEase(arrangeEase)
                .SetDelay(delay)
                .SetUpdate(false)
                .SetId(ArrangeTweenId(enemy));
        }

        private void MoveEnemiesToTargets(List<Vector3> targetPositions, bool useStagger)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                BattleEnemy enemy = enemies[i];
                if (enemy == null) continue;

                if (!_arrangedEnemies.Contains(enemy))
                {
                    enemy.transform.position = targetPositions[i] + (Vector3)spawnOffset;
                    _arrangedEnemies.Add(enemy);
                }

                float delay = useStagger && arrangeStagger > 0f ? i * arrangeStagger : 0f;
                MoveEnemyToTarget(enemy, targetPositions[i], delay);
            }
        }

        private void KillAllArrangeTweens()
        {
            foreach (KeyValuePair<BattleEnemy, Tween> kv in _arrangeTweens)
            {
                if (kv.Value != null && kv.Value.IsActive())
                    kv.Value.Kill();
            }
            
            _arrangeTweens.Clear();
        }

        public void TryStartEnemyAttackSequence()
        {
            if (_isAttackSequenceRunning) return;
            
            if (_pendingSpawnCount == 0 && _arrangeDirty && _initialArrangeDone)
            {
                _arrangeDirty = false;
                EnemyArrangementStrategy.SortEnemiesList(enemies); 
                ArrangeEnemies(true);
            }

            _seqCo = StartCoroutine(EnemyAttackSequence());
        }

        private void OnEnemyTurnDone(EnemyTurnDoneEvent evt)
        {
            if (!_isAttackSequenceRunning || _waitingBattleEnemy == null || evt.BattleEnemy == null) return;

            if (ReferenceEquals(evt.BattleEnemy, _waitingBattleEnemy))
                _waitingDone = true;
        }

        private IEnumerator EnemyAttackSequence()
        {
            _isAttackSequenceRunning = true;
            
            yield return new WaitUntil(() => _pendingSpawnCount == 0);
            
            if (_arrangeDirty && _initialArrangeDone)
            {
                _arrangeDirty = false;
                EnemyArrangementStrategy.SortEnemiesList(enemies); 
                ArrangeEnemies(true);
                yield return new WaitForSeconds(arrangeDuration);
            }
            
            if (!_initialArrangeDone)
            {
                yield return new WaitUntil(() => _initialArrangeDone);
                yield return new WaitForSeconds(arrangeDuration); 
            }
            else
            {
                yield return new WaitForSeconds(arrangeDuration);
            }
            
            yield return new WaitForSeconds(attackDelay);

            List<BattleEnemy> activeEnemies = new(enemies);

            foreach (BattleEnemy enemy in activeEnemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead || enemy.IsFreeze || enemy.enemySO.attackSkills.Length == 0)
                {
                    if (enemy != null && enemy.enemySO.attackSkills.Length == 0)
                        Debug.Log($"{enemy.gameObject.name}은 공격 스킬 없음 - 턴 스킵");
                    continue;
                }

                _waitingBattleEnemy = enemy;
                _waitingDone = false;
                
                enemy.SendBTState(BattleEnemyState.Move);
                
                yield return new WaitUntil(() => _waitingDone || 
                                                 _waitingBattleEnemy == null || 
                                                 !_waitingBattleEnemy.gameObject.activeInHierarchy || 
                                                 _waitingBattleEnemy.IsDead);

                _waitingBattleEnemy = null;

                if (enemy != null && !enemy.IsDead) 
                {
                    yield return new WaitForSeconds(attackDelay);
                }
            }
            
            //TickEnemyBuffsOnce(activeEnemies);
            //yield return new WaitForSeconds(0.3f);

            _isAttackSequenceRunning = false;
            _seqCo = null;

            if (_arrangeDirty)
            {
                _arrangeDirty = false;
                EnemyArrangementStrategy.SortEnemiesList(enemies); 
                ArrangeEnemies(_initialArrangeDone);
            }

            if (enemies.Any(e => e != null && !e.IsDead && e.gameObject.activeInHierarchy))
            {
                _turnManagerCache?.NextTurn();
            }
            else if (!_battleEndRaised)
            {
                Debug.Log("턴 전환 중단");
            }
        }

        public void ResetAttackSequenceFlag() => _isAttackSequenceRunning = false;
        
        private void TickEnemyBuffsOnce(List<BattleEnemy> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var enemy = list[i];
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                enemy.buffModule?.UpdateTime();
            }
        }

        private void HandleTurnStarted(bool isPlayerTurn)
        {
            if (isPlayerTurn)
            {
                Debug.Log("플레이어 턴 시작: 적들에게 걸린 도트 데미지를 일괄 적용합니다.");
                TickEnemyBuffsOnce(enemies);
            }
        }
    }
}