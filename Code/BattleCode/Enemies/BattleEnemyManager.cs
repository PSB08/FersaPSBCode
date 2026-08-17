using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using CIW.Code.System.Events;
using PSB.Code.CoreSystem.Events;
using PSB_Lib.Dependencies;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.BTs.Events;
using PSB.Code.BattleCode.Enemies.Mechanics;
using PSB.Code.BattleCode.Enemies.PhaseBreak;
using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.Players;
using PSW.Code.EventBus;
using UnityEngine;
using Work.CSH.Scripts.Managers;

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
        [SerializeField, Min(0f)] private float enemyTurnDoneTimeout = 8f;
        
        [Header("배치 범위 설정")]
        [SerializeField] private BatchSize batchSize;
        
        [Header("배치 설정")]
        [SerializeField] private Vector2 startPosition = new(0f, 0f);
        [SerializeField] private Vector2 cellSize = new(1.5f, 1.5f);
        [SerializeField, Range(1, 3)] private int maxActiveEnemyCount = 3;
        [SerializeField, Range(0f, 1f)] private float backLineExtraX = 0.4f;
        
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
        private float _turnDoneElapsed;
        private Coroutine _seqCo;
        private readonly EnemyBattleMechanicScope _mechanicScope =
            new EnemyBattleMechanicScope();
        
        [Inject] private PlayerManager _playerManager;
        
        public EnemyBattleMechanicScope MechanicScope => _mechanicScope;
        
        private float ArrangeWaitTime
        {
            get
            {
                int enemyCount = enemies != null ? enemies.Count : 0;
                int staggerCount = Mathf.Max(0, enemyCount - 1);
                return arrangeDuration + (staggerCount * arrangeStagger);
            }
        }
        
        private void Awake()
        {
            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);
        }
        
        private void OnEnable()
        {
            Bus<EnemyTurnDoneEvent>.OnEvent += OnEnemyTurnDone;
            
            ResetState();
            _isFirstArrange = true;
        }
        
        private void OnDisable()
        {
            Bus<EnemyTurnDoneEvent>.OnEvent -= OnEnemyTurnDone;
            
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
        
        public void BeginEncounter()
        {
            ResetState();
            _mechanicScope.ClearValues();
            _isFirstArrange = true;
        }
        
        public void StartBattleEncounter()
        {
            StartCoroutine(InitArrangeCoroutine());
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
            
            EnemyArrangementStrategy.SortEnemiesList(enemies);
            
            bool doTween = !_isFirstArrange;
            List<Vector3> targets = EnemyArrangementStrategy.ComputePositions
                (enemies, GetMaxActiveEnemyCount(), GetArrangeStartPosition(), cellSize, backLineExtraX, batchSize);
            
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
                yield return new WaitForSeconds(ArrangeWaitTime);
                _arrangeDirty = true;
            }
            
            _initialArrangeDone = true;
        }
        
        private void Update()
        {
            if (!_battleEndRaised && _initialArrangeDone && enemies.Count <= 0 && _waitingQueue.Count <= 0 && _pendingSpawnCount <= 0)
            {
                _battleEndRaised = true;
                Debug.Log("전투 적 다 죽었다");
                Bus<BattleEnd>.Raise(new BattleEnd(true));
            }
        }
        
        public void Register(BattleEnemy battleEnemy)
        {
            if (battleEnemy == null || enemies.Contains(battleEnemy) 
                                    || _waitingQueue.Contains(battleEnemy)) return;
            
            battleEnemy.transform.position = (Vector3)startPosition + (Vector3)spawnOffset;
            
            SetEnemyVisible(battleEnemy, false);
            
            if (enemies.Count >= GetMaxActiveEnemyCount())
            {
                _waitingQueue.Enqueue(battleEnemy);
                return;
            }
            
            AddNewEnemyToField(battleEnemy);
        }
        
        public void Unregister(BattleEnemy battleEnemy, bool waitForDeathExit = true)
        {
            if (battleEnemy == null) return;
            
            //아직 필드에 나오지 않은 대기열 적이면 큐에서만 제거
            if (_waitingQueue.Contains(battleEnemy))
            {
                //Queue는 직접 Remove가 없어서 해당 적만 제외한 새 큐
                _waitingQueue = new Queue<BattleEnemy>(_waitingQueue.Where(x => x != battleEnemy));
                //대기열 제거만 끝내면 필드 정렬은 건드릴 필요 X
                return;
            }
            
            //필드에 있는 적 목록에서 제거할 위치를 찾음
            int deadIndex = enemies.IndexOf(battleEnemy);
            //이미 제거된 적이면 중복 Unregister이므로 종료
            if (deadIndex < 0) return;
            
            //해당 적에게 걸린 정렬 트윈이 남아 있으면 죽은 뒤 위치 이동하지 않게 끊기
            if (DOTween.instance != null)
                DOTween.Kill(ArrangeTweenId(battleEnemy));
            
            //트윈 추적 딕셔너리에서도 제거
            _arrangeTweens.Remove(battleEnemy);
            
            //실제 필드 적 목록에서 제거
            enemies.RemoveAt(deadIndex);
            //정렬 완료 목록에서도 제거해서 다음 정렬 때 죽은 적을 기준으로 보지 않게
            _arrangedEnemies.Remove(battleEnemy); 
            
            //공격 완료를 기다리던 적이 죽은 경우, 대기 루프를 풀어 다음 흐름으로 넘김
            if (_waitingBattleEnemy == battleEnemy)
            {
                //기다리던 대상 참조 비우기
                _waitingBattleEnemy = null;
                //WaitForEnemyTurnDone 루프가 끝나도록 완료 처리
                _waitingDone = true;
            }
            
            //대기열에 다음 적이 있으면 죽은 적 자리에 교체 투입
            if (_waitingQueue.Count > 0)
            {
                if (waitForDeathExit)
                {
                    //공격 시퀀스가 새 적 투입 완료 전 진행하지 않게 스폰 대기 카운트 증가
                    _pendingSpawnCount++;
                    //죽은 적이 비활성/파괴된 뒤 새 적을 넣는 코루틴을 시작
                    StartCoroutine(WaitDeathSpawnCoroutine(battleEnemy, deadIndex));
                }
                else
                {
                    //즉시 교체가 필요한 특수 호출이면 대기열에서 다음 적을 바로 꺼냄
                    BattleEnemy nextEnemy = _waitingQueue.Dequeue();
                    //죽은 적이 있던 인덱스에 새 적을 즉시 추가
                    AddNewEnemyToField(nextEnemy, deadIndex);
                }
            }
            else if (!_initialArrangeDone)
            {
                //첫 배치 중이면 트윈 없이 현재 목록 기준 위치를 바로 맞춤
                ArrangeEnemies(false);
            }
            else
            {
                //대기열은 없지만 빈 칸이 생겼으므로 다음 안전 지점에서 재정렬
                _arrangeDirty = true;
            }
            
            //UI, 타겟 선택 쪽이 적 목록 변화를 알 수 있게 이벤트를 보냄
            Bus<EnemyListChanged>.Raise(new EnemyListChanged());
        }
        
        private void AddNewEnemyToField(BattleEnemy battleEnemy, int insertIndex = -1)
        {
            //지정된 삽입 위치가 유효하면 그 자리에 적을 넣음
            if (insertIndex >= 0 && insertIndex <= enemies.Count)
            {
                //죽은 적이 있던 위치에 대기열 적을 넣어 배치 순서를 유지
                enemies.Insert(insertIndex, battleEnemy);
            }
            else
            {
                //삽입 위치가 없으면 맨 뒤에 추가
                enemies.Add(battleEnemy);
            }
            
            //첫 배치가 끝난 뒤 추가된 적은 화면 밖에서 목표 위치로 이동
            if (_initialArrangeDone)
            {
                _arrangeDirty = true; 
                
                //현재 적 목록 기준으로 모든 목표 위치를 다시 계산
                List<Vector3> targets = EnemyArrangementStrategy.ComputePositions
                    (enemies, GetMaxActiveEnemyCount(), GetArrangeStartPosition(), cellSize, backLineExtraX, batchSize);
                //방금 추가한 적의 현재 목록 인덱스를 찾기
                int idx = enemies.IndexOf(battleEnemy);
                
                //목표 위치 계산이 성공했고 인덱스도 유효하면 새 적을 스폰 위치에서 이동
                if (targets != null && idx >= 0 && idx < targets.Count)
                {
                    //새 적은 목표 위치 오른쪽 오프셋에서 등장하게 배치
                    battleEnemy.transform.position = targets[idx] + (Vector3)spawnOffset;
                    //이미 스폰 오프셋을 적용했으므로 중복 오프셋을 피하려고 정렬 목록에 추가
                    _arrangedEnemies.Add(battleEnemy);
                    //목표 위치까지 이동 트윈을 시작합니다.
                    MoveEnemyToTarget(battleEnemy, targets[idx], 0f);
                }
                
                //위치 세팅 후 렌더러를 켜서 새 적이 보이게
                SetEnemyVisible(battleEnemy, true);
            }
            
            //새 적의 TurnManager를 캐싱해서 턴 전환/전투 종료 조건을 계속 사용할 수 있게
            CacheTurnManager(battleEnemy);
            //UI/타겟 선택 쪽에 적 목록 갱신을 알림
            Bus<EnemyListChanged>.Raise(new EnemyListChanged());
        }
        
        private IEnumerator WaitDeathSpawnCoroutine(BattleEnemy dyingEnemy, int insertIndex)
        {
            //finally에서 카운트를 반드시 내리기 위해 try/finally로 감쌈
            try
            {
                //죽은 적 오브젝트가 아직 남아 있으면 실제 비활성/파괴까지 기다림
                if (dyingEnemy != null)
                {
                    //Unity null 판정 또는 비활성 상태가 될 때까지 새 적 투입을 늦춤
                    yield return new WaitUntil(() => dyingEnemy == null || 
                                                     !dyingEnemy.gameObject.activeInHierarchy);
                }
                
                //기다리는 동안 큐가 비었을 수 있으니 남은 대기열이 있을 때만 투입
                if (_waitingQueue.Count > 0)
                {
                    //다음 대기 적을 큐에서 꺼냄
                    BattleEnemy nextEnemy = _waitingQueue.Dequeue();
                    //죽은 적이 있던 인덱스에 새 적을 추가
                    AddNewEnemyToField(nextEnemy, insertIndex);
                    
                    //새 적 등장 이동이 끝날 때까지 기다려 공격 시퀀스와 정렬이 겹치지 않게
                    yield return new WaitForSeconds(ArrangeWaitTime);
                }
            }
            finally
            {
                //스폰 대기 카운트를 내려 공격 시퀀스가 다시 진행될 수 있게
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
            if (_battleEndRaised) return true;
            
            foreach (var e in enemies)
            {
                if (e != null && e.gameObject.activeInHierarchy && !e.IsDead)
                    return false;
            }
            
            if (_waitingQueue.Count > 0 || _pendingSpawnCount > 0) return false;
            
            return true;
        }
        
        private void ArrangeEnemies(bool tween)
        {
            List<Vector3> targets = EnemyArrangementStrategy.ComputePositions
                (enemies, GetMaxActiveEnemyCount(), GetArrangeStartPosition(), cellSize, backLineExtraX, batchSize);
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
            enemy?.GetModule<EnemyAttack>()?.PrepareForExternalReposition();
            
            if (killPreviousArrangeTween && DOTween.instance != null)
                DOTween.Kill(ArrangeTweenId(enemy));
            
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
            //-- 이미 적 공격 코루틴이 돌고 있으면 중복 실행을 막습니다.
            if (_isAttackSequenceRunning) return;
            
            //-- 스폰/정렬 대기는 EnemyAttackSequence 안에서 순서대로 처리합니다.
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
            //적 공격 시퀀스가 시작됐음을 표시해서 재진입 막음
            _isAttackSequenceRunning = true;
            
            //죽은 적 교체 스폰과 정렬이 끝나기 전에는 공격을 시작하지 않음
            yield return WaitForSpawnAndArrangeBeforeAttack(true);
            
            //정렬 후 바로 공격하지 않고 기존 공격 시작 딜레이를 유지
            yield return new WaitForSeconds(attackDelay);
            
            //이번 적 턴 시작 시점의 적 목록을 스냅샷으로 잡아 새로 난입한 적은 다음 턴부터 행동하게
            List<BattleEnemy> activeEnemies = new(enemies);
            
            foreach (BattleEnemy enemy in activeEnemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead || enemy.IsFreeze)
                {
                    continue;
                }
                
                EnemyMechanicController mechanicController = enemy.MechanicController;
                
                if (mechanicController != null && mechanicController.IsInitialized)
                {
                    yield return mechanicController.ResolveBeforeAction();
                }
                else
                {
                    EnemyPhaseBreakController phaseBreakController =
                        enemy.GetComponent<EnemyPhaseBreakController>();
                    
                    if (phaseBreakController != null && phaseBreakController.HasPendingBreak)
                        yield return phaseBreakController.ResolveBeforeAttack();
                }
                
                if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.IsDead)
                    continue;
                
                EnemyAttack enemyAttack = enemy.GetModule<EnemyAttack>();
                if (enemyAttack == null || !enemyAttack.HasAttackSkill)
                {
                    continue;
                }
                
                enemyAttack.ClearCachedTurnStartPosition();
                
                _waitingBattleEnemy = enemy;
                _waitingDone = false;
                _turnDoneElapsed = 0f;
                
                if (!enemy.TrySendBTState(BattleEnemyState.Move))
                {
                    //BT 상태 전송 실패 시 턴 완료 이벤트를 기다리며 멈추지 않도록 바로 다음 적으로
                    _waitingBattleEnemy = null;
                    _waitingDone = false;
                    continue;
                }
                
                yield return WaitForEnemyTurnDone(enemy);
                
                _waitingBattleEnemy = null;
                
                if (enemy != null && !enemy.IsDead) 
                {
                    yield return new WaitForSeconds(attackDelay);
                }
            }
            
            //TickEnemyBuffsOnce(activeEnemies);
            //yield return new WaitForSeconds(0.3f);
            
            //공격 도중 죽은 적 자리에 새 적이 들어왔다면 턴을 넘기기 전에 스폰, 정렬을 끝냄
            yield return WaitForSpawnAndArrangeBeforeAttack(false);
            
            //적 공격 시퀀스 종료 상태로 되돌림
            _isAttackSequenceRunning = false;
            //현재 코루틴 참조를 비워 다음 적 턴에 새 코루틴을 시작할 수 있게
            _seqCo = null;
            
            if (enemies.Any(e => e != null && !e.IsDead && e.gameObject.activeInHierarchy))
            {
                _turnManagerCache?.NextTurn();
            }
            else if (!_battleEndRaised)
            {
            }
        }
        
        private IEnumerator WaitForEnemyTurnDone(BattleEnemy enemy)
        {
            while (!_waitingDone && !IsWaitingEnemyGone())
            {
                if (enemyTurnDoneTimeout > 0f)
                {
                    _turnDoneElapsed += Time.deltaTime;
                    if (_turnDoneElapsed >= enemyTurnDoneTimeout)
                    {
                        Debug.LogWarning(
                            $"[BattleEnemyManager] {enemy?.name} enemy turn done timeout. Force continuing enemy sequence.",
                            enemy);
                        
                        enemy?.SendBTState(BattleEnemyState.Idle);
                        _waitingDone = true;
                        yield break;
                    }
                }
                
                yield return null;
            }
        }
        
        public void ResetTurnDoneTimeout(BattleEnemy enemy)
        {
            if (enemy != null && ReferenceEquals(enemy, _waitingBattleEnemy))
                _turnDoneElapsed = 0f;
        }
        
        private bool IsWaitingEnemyGone()
            => _waitingBattleEnemy == null ||
               !_waitingBattleEnemy.gameObject.activeInHierarchy ||
               _waitingBattleEnemy.IsDead;
        
        private IEnumerator WaitForSpawnAndArrangeBeforeAttack(bool waitWhenClean)
        {
            //죽은 적 자리에 대기열 적을 넣는 코루틴이 남아 있으면 먼저 모두 기다림
            yield return new WaitUntil(() => _pendingSpawnCount == 0);
            
            //첫 배치가 아직 끝나지 않았으면 첫 배치 완료까지 기다림
            if (!_initialArrangeDone)
            {
                //InitArrangeCoroutine이 _initialArrangeDone을 true로 만들 때까지 대기
                yield return new WaitUntil(() => _initialArrangeDone);
                //첫 배치 트윈이 화면상으로 끝날 시간을 한 번 더 대기
                yield return new WaitForSeconds(ArrangeWaitTime);
                //초기 배치 대기를 끝냈으므로 호출자에게 제어를 돌려줌
                yield break;
            }
            
            //새 적 생성/사망 제거로 정렬이 더러워졌으면 여기서 한 번만 재정렬
            if (_arrangeDirty)
            {
                _arrangeDirty = false; //같은 정렬 요청이 반복 실행되지 않게 플래그를 먼저 내림
                EnemyArrangementStrategy.SortEnemiesList(enemies); //현재 살아 있는 적 순서 기준으로 정렬 리스트를 다시 맞춤
                
                ArrangeEnemies(true); //실제 화면 위치 이동 트윈을 시작
                
                yield return new WaitForSeconds(ArrangeWaitTime); //이동 트윈과 스태거가 끝날 때까지 기다린 뒤 다음 흐름으로 넘어감
                yield break;  //정렬 처리를 끝냈으므로 호출자에게 제어를 돌려줌
            }
            
            if (waitWhenClean)
                yield return new WaitForSeconds(arrangeDuration);
        }
        
        public void ResetAttackSequenceFlag() => _isAttackSequenceRunning = false;
        
        private int GetMaxActiveEnemyCount()
        {
            return Mathf.Clamp(maxActiveEnemyCount, 1, 3);
        }
        
        private Vector2 GetArrangeStartPosition()
        {
            Vector2 result = startPosition;
            
            if (_playerManager != null && _playerManager.BattlePlayer != null)
                result.y = _playerManager.BattlePlayer.transform.position.y;
            
            return result;
        }
        
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
                TickEnemyBuffsOnce(enemies);
            }
        }
        
        private void OnDestroy()
        {
            _mechanicScope.Dispose();
        }
    
    }
}
