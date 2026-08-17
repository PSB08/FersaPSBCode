using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CIW.Code;
using CIW.Code.System.Events;
using CSH.Scripts.Items;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Events;
using PSB.Code.CoreSystem.Events;
using PSB_Lib.Dependencies;
using PSW.Code.EventBus;
using UnityEngine;
using Work.CSH.Scripts.Managers;
using YIS.Code.Modules;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Players
{
    public class PlayerTargetSelector : MonoBehaviour, IModule
    {
        private BattlePlayer _player;
        private TurnManagerSO _turnManagerCache;
        
        private BattleEnemy _currentTarget;
        private EnemyAttackPreviewVfx _currentAttackPreviewVfx;

        private Coroutine _bootstrapCo;

        [Inject] private BattleEnemyManager _battleEnemyManager;

        private int _currentIndex = -1;
        private bool _inputLocked;
        
        private int _previewRange = 0;
        
        private SkillDataSO[] _currentPreviewSkills;
        private bool[] _currentPreviewSetIndex;
        private int _currentPreviewSlotIndex = -1;

        private readonly List<EnemyRangePreviewVfx> _previewVfx = new();
        private PreviewDamageCalculator _previewCalc;

        public void Initialize(ModuleOwner owner)
        {
            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);

            _player = owner as BattlePlayer;
            if (_player != null)
                _previewCalc = _player.GetModule<PreviewDamageCalculator>();
        }

        private void OnEnable()
        {
            _inputLocked = false;

            if (_player == null)
                _player = GetComponent<BattlePlayer>();

            _turnManagerCache = _player != null ? _player.TurnManager : null;

            if (_turnManagerCache != null)
            {
                _turnManagerCache.OnTurnStarted += HandleTurnStart;
                _turnManagerCache.OnTurnEnded += HandleTurnEnd;
            }

            if (_player != null && _player.PlayerInput != null)
            {
                _player.PlayerInput.OnNextPressed += MoveDown;
                _player.PlayerInput.OnBeforePressed += MoveUp;
            }

            Bus<EnemyListChanged>.OnEvent += HandleEnemyListChanged;
            Bus<BeginAttackEvent>.OnEvent += HandleBeginAttack;

            Bus<SkillRangePreviewEvent>.OnEvent += HandleSkillRangePreview;
            Bus<SkillDamagePreviewEvent>.OnEvent += HandleSkillDamagePreview;

            DisableAllEnemyRangeVfx();
            ClearHighlight();

            _currentTarget = null;
            _currentIndex = -1;

            if (_bootstrapCo != null) StopCoroutine(_bootstrapCo);
            _bootstrapCo = StartCoroutine(CoBootstrapSelect());
        }

        private void OnDisable()
        {
            if (_bootstrapCo != null)
            {
                StopCoroutine(_bootstrapCo);
                _bootstrapCo = null;
            }

            if (_turnManagerCache != null)
            {
                _turnManagerCache.OnTurnStarted -= HandleTurnStart;
                _turnManagerCache.OnTurnEnded -= HandleTurnEnd;
            }

            if (_player != null && _player.PlayerInput != null)
            {
                _player.PlayerInput.OnNextPressed -= MoveDown;
                _player.PlayerInput.OnBeforePressed -= MoveUp;
            }

            Bus<EnemyListChanged>.OnEvent -= HandleEnemyListChanged;
            Bus<BeginAttackEvent>.OnEvent -= HandleBeginAttack;

            Bus<SkillRangePreviewEvent>.OnEvent -= HandleSkillRangePreview;
            Bus<SkillDamagePreviewEvent>.OnEvent -= HandleSkillDamagePreview;

            ClearHighlight();
            DisableAllEnemyRangeVfx();

            _currentTarget = null;
            _currentIndex = -1;
            _previewRange = 0;
        }

        private void HandleSkillRangePreview(SkillRangePreviewEvent evt)
        {
            _previewRange = Mathf.Max(0, evt.Range);
            RefreshPreviewVfx();
        }

        private void HandleSkillDamagePreview(SkillDamagePreviewEvent evt)
        {
            _currentPreviewSkills = evt.SkillDatas;
            _currentPreviewSetIndex = evt.SetSkillIndex;
            _currentPreviewSlotIndex = evt.PreviewSlotIndex;
            RefreshPreviewVfx();
        }

        private void HandleBeginAttack(BeginAttackEvent evt)
        {
            ClearHighlight();
            DisableAllEnemyRangeVfx();
            _inputLocked = true;
            
            _currentPreviewSkills = null;
            _currentPreviewSetIndex = null;
            _currentPreviewSlotIndex = -1;
            _previewRange = 0;
        }

        public void UnlockInput()
        {
            _inputLocked = false;
            RefreshPreviewVfx();
        }

        private IEnumerator CoBootstrapSelect()
        {
            yield return null;
            _bootstrapCo = null;

            if (_inputLocked) yield break;
            TrySelectAndHighlightFirstEnemy();

            RefreshPreviewVfx();
        }

        private void HandleEnemyListChanged(EnemyListChanged evt)
        {
            if (_inputLocked) return;

            TrySelectAndHighlightFirstEnemy();
            RefreshPreviewVfx();
        }

        private void HandleTurnStart(bool isPlayerTurn)
        {
            if (!isPlayerTurn)
            {
                _inputLocked = true;
                ClearHighlight();
                DisableAllEnemyRangeVfx();
                _currentTarget = null;
                _currentIndex = -1;
                return;
            }

            _inputLocked = false;

            DisableAllEnemyRangeVfx();
            SelectFirstEnemy();
            RefreshPreviewVfx();
        }

        private void HandleTurnEnd(bool isPlayerTurn)
        {
            if (!isPlayerTurn) return;

            _inputLocked = true;
            ClearHighlight();
            DisableAllEnemyRangeVfx();
            
            _currentPreviewSkills = null;
            _currentPreviewSetIndex = null;
            _currentPreviewSlotIndex = -1;
            _previewRange = 0;
        }

        private void SelectFirstEnemy()
        {
            var enemies = _battleEnemyManager != null ? _battleEnemyManager.GetEnemies() : null;
            if (enemies == null || enemies.Count == 0)
            {
                ClearSelection();
                return;
            }

            if (TrySelectTauntTarget(enemies))
                return;

            if (TryGetTargetableIndexFrom(enemies, 0, out int targetIndex))
            {
                TrySelectEnemyAt(enemies, targetIndex);
                return;
            }

            ClearSelection();
            ShowFirstEnemyInfoOnly(enemies);
        }

        private void TrySelectAndHighlightFirstEnemy()
        {
            var enemies = _battleEnemyManager != null ? _battleEnemyManager.GetEnemies() : null;
            if (enemies == null || enemies.Count == 0)
            {
                ClearSelection();
                return;
            }

            if (TrySelectTauntTarget(enemies))
                return;

            if (_currentIndex < 0) _currentIndex = 0;
            _currentIndex = Mathf.Clamp(_currentIndex, 0, enemies.Count - 1);

            if (_currentTarget != null && SkillTargetingUtil.CanBeDirectTarget(_currentTarget))
            {
                int idx = IndexOf(enemies, _currentTarget);
                if (idx >= 0)
                {
                    TrySelectEnemyAt(enemies, idx);
                    return;
                }
            }

            if (TryGetTargetableIndexFrom(enemies, _currentIndex, out int targetIndex))
            {
                TrySelectEnemyAt(enemies, targetIndex);
                return;
            }

            ClearSelection();
            ShowFirstEnemyInfoOnly(enemies);
        }

        public void MoveUp()
        {
            if (_inputLocked) return;

            var enemies = _battleEnemyManager != null ? _battleEnemyManager.GetEnemies() : null;
            if (enemies == null || enemies.Count == 0) return;

            if (TrySelectTauntTarget(enemies))
            {
                RefreshPreviewVfx();
                return;
            }

            if (!TryGetNextTargetableIndex(enemies, _currentIndex, 1, out int nextIndex, true))
            {
                ClearSelection();
                return;
            }

            TrySelectEnemyAt(enemies, nextIndex);
            RefreshPreviewVfx();
        }

        public void MoveDown()
        {
            if (_inputLocked) return;

            var enemies = _battleEnemyManager != null ? _battleEnemyManager.GetEnemies() : null;
            if (enemies == null || enemies.Count == 0) return;

            if (TrySelectTauntTarget(enemies))
            {
                RefreshPreviewVfx();
                return;
            }

            if (!TryGetNextTargetableIndex(enemies, _currentIndex, -1, out int nextIndex, true))
            {
                ClearSelection();
                return;
            }

            TrySelectEnemyAt(enemies, nextIndex);
            RefreshPreviewVfx();
        }

        private void HighlightEnemy(BattleEnemy target)
        {
            if (target == null)
                return;

            if (!SkillTargetingUtil.CanBeDirectTarget(target))
            {
                ClearHighlight();
                return;
            }

            ClearHighlight();

            var eap = target.GetModule<EnemyAttackPreviewVfx>();
            if (eap == null) 
                return;

            _currentAttackPreviewVfx = eap;
            eap.SetPreview(target);

            _currentTarget = target;
            Bus<EnemyHoverInfoEvent>.Raise(new EnemyHoverInfoEvent(target, true));
            Bus<SetItemContextEvent>.Raise(new SetItemContextEvent()
                { context = { user = _player, target = _currentTarget, isField = false }});
        }

        private void ClearHighlight()
        {
            if (_currentAttackPreviewVfx != null)
            {
                _currentAttackPreviewVfx.ClearPreview();
                _currentAttackPreviewVfx = null;
            }
        }

        public BattleEnemy GetCurrentTarget()
        {
            var enemies = _battleEnemyManager != null ? _battleEnemyManager.GetEnemies() : null;
            if (enemies == null || enemies.Count == 0)
            {
                ClearSelection();
                return null;
            }

            if (SkillTargetingUtil.TryGetTauntTargetIndex(enemies, out int tauntIndex))
            {
                TrySelectEnemyAt(enemies, tauntIndex);
                return _currentTarget;
            }

            if (_currentTarget != null && SkillTargetingUtil.CanBeDirectTarget(_currentTarget))
            {
                int idx = IndexOf(enemies, _currentTarget);
                if (idx >= 0)
                    return _currentTarget;
            }

            if (_currentIndex < 0) _currentIndex = 0;

            if (TryGetTargetableIndexFrom(enemies, _currentIndex, out int targetIndex))
            {
                TrySelectEnemyAt(enemies, targetIndex);
                return _currentTarget;
            }

            ClearSelection();
            return null;
        }

        public int GetCurrentTargetIndex()
        {
            var enemies = _battleEnemyManager != null ? _battleEnemyManager.GetEnemies() : null;
            if (enemies == null || enemies.Count == 0) 
                return -1;

            if (SkillTargetingUtil.TryGetTauntTargetIndex(enemies, out int tauntIndex))
            {
                _currentIndex = tauntIndex;
                _currentTarget = enemies[tauntIndex];
                return tauntIndex;
            }

            if (_currentTarget != null && SkillTargetingUtil.CanBeDirectTarget(_currentTarget))
            {
                int idx = IndexOf(enemies, _currentTarget);
                if (idx >= 0) 
                    return idx;
            }

            if (TryGetTargetableIndexFrom(enemies, _currentIndex, out int targetIndex))
            {
                _currentIndex = targetIndex;
                _currentTarget = enemies[targetIndex];
                return targetIndex;
            }

            return -1;
        }
        
        private bool TrySelectTauntTarget(IReadOnlyList<BattleEnemy> enemies)
        {
            if (!SkillTargetingUtil.TryGetTauntTargetIndex(enemies, out int tauntIndex))
                return false;

            return TrySelectEnemyAt(enemies, tauntIndex);
        }

        private bool TrySelectEnemyAt(IReadOnlyList<BattleEnemy> enemies, int index)
        {
            if (enemies == null || index < 0 || index >= enemies.Count)
                return false;

            var target = enemies[index];
            if (!SkillTargetingUtil.CanBeDirectTarget(target))
                return false;

            _currentIndex = index;
            _currentTarget = target;

            HighlightEnemy(_currentTarget);
            return true;
        }

        private bool TryGetTargetableIndexFrom(IReadOnlyList<BattleEnemy> enemies, int startIndex, out int targetIndex)
        {
            targetIndex = -1;

            if (enemies == null || enemies.Count == 0)
                return false;

            if (startIndex < 0)
                startIndex = 0;

            startIndex %= enemies.Count;

            for (int i = 0; i < enemies.Count; i++)
            {
                int index = (startIndex + i) % enemies.Count;
                var enemy = enemies[index];

                if (!SkillTargetingUtil.CanBeDirectTarget(enemy))
                    continue;

                targetIndex = index;
                return true;
            }

            return false;
        }

        private bool TryGetNextTargetableIndex(IReadOnlyList<BattleEnemy> enemies, int currentIndex, 
            int direction, out int targetIndex, bool logBlockedTarget = false)
        {
            targetIndex = -1;

            if (enemies == null || enemies.Count == 0)
                return false;

            direction = direction >= 0 ? 1 : -1;

            int startIndex = currentIndex;
            if (startIndex < 0 || startIndex >= enemies.Count)
                startIndex = direction > 0 ? -1 : enemies.Count;

            bool hasLoggedBlock = false;

            for (int i = 1; i <= enemies.Count; i++)
            {
                int index = (startIndex + direction * i) % enemies.Count;
                if (index < 0)
                    index += enemies.Count;

                var enemy = enemies[index];

                bool canTarget = SkillTargetingUtil.CanBeDirectTarget(enemy, logBlockedTarget && !hasLoggedBlock);

                if (!canTarget)
                {
                    hasLoggedBlock = true;
                    continue;
                }

                targetIndex = index;
                return true;
            }

            return false;
        }

        private void ClearSelection()
        {
            _currentIndex = -1;
            _currentTarget = null;
            ClearHighlight();
            DisableAllEnemyRangeVfx();
        }

        private int IndexOf(IReadOnlyList<BattleEnemy> list, BattleEnemy target)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == target) 
                    return i;
            return -1;
        }

        private void RefreshPreviewVfx()
        {
            if (_inputLocked)
            {
                DisableAllEnemyRangeVfx();
                return;
            }

            if (_battleEnemyManager == null)
            {
                DisableAllEnemyRangeVfx();
                return;
            }

            if (_currentPreviewSlotIndex < 0 && _previewRange <= 0)
            {
                DisableAllEnemyRangeVfx();
                return;
            }

            var enemies = _battleEnemyManager.GetEnemies();
            int centerIndex = GetCurrentTargetIndex();

            if (enemies == null || enemies.Count == 0 || centerIndex < 0)
            {
                DisableAllEnemyRangeVfx();
                return;
            }

            SetPreviewVfx(enemies, centerIndex);
        }

        private void SetPreviewVfx(IReadOnlyList<BattleEnemy> enemies, int centerIndex)
        {
            DisableAllEnemyRangeVfx();
            if (enemies == null) return;

            List<Entity>[] actualSlotTargets = null;
            
            if (_currentPreviewSkills != null && _currentPreviewSetIndex != null)
            {
                actualSlotTargets = new List<Entity>[_currentPreviewSkills.Length];
                for (int i = 0; i < _currentPreviewSkills.Length; i++)
                {
                    if (_currentPreviewSkills[i] != null && _currentPreviewSetIndex[i])
                    {
                        var defaultTargets = SkillTargetingUtil.GetTargetsByRange(enemies, centerIndex, _currentPreviewSkills[i].range);
                        
                        actualSlotTargets[i] = _previewCalc != null 
                            ? _previewCalc.GetActualSimulatedTargets
                            (i, _currentPreviewSkills, _currentPreviewSetIndex, defaultTargets)
                            : defaultTargets;
                    }
                }
            }

            foreach (var enemy in enemies)
            {
                var vfx = enemy.GetModule<EnemyRangePreviewVfx>();
                if (vfx == null) continue;

                bool[] isHitBySlot = null;
                bool isHitByAnything = false;

                if (_currentPreviewSkills != null && _currentPreviewSetIndex != null && actualSlotTargets != null)
                {
                    isHitBySlot = new bool[_currentPreviewSkills.Length];
                    for (int i = 0; i < _currentPreviewSkills.Length; i++)
                    {
                        if (actualSlotTargets[i] != null && actualSlotTargets[i].Contains(enemy))
                        {
                            isHitBySlot[i] = true;
                            isHitByAnything = true;
                        }
                    }
                }

                bool isInCurrentHoverRange = false;
                if (_previewRange > 0 || (_currentPreviewSkills != null && _currentPreviewSlotIndex >= 0))
                {
                    var hoverTargets = SkillTargetingUtil.GetTargetsByRange(enemies, centerIndex, Mathf.Max(0, _previewRange));
                    if (hoverTargets != null && hoverTargets.Contains(enemy))
                    {
                        isInCurrentHoverRange = true;
                    }
                }

                if (isHitByAnything || isInCurrentHoverRange)
                {
                    float simulatedDmg = 0f;
                    float simulatedAccDmg = 0f;

                    if (_previewCalc != null)
                    {
                        var result = _previewCalc.CalculatePreviewForTarget(enemy, _currentPreviewSkills, _currentPreviewSetIndex, _currentPreviewSlotIndex, isHitBySlot);
                        simulatedDmg = result.curDmg;
                        simulatedAccDmg = result.accDmg;
                    }

                    vfx.SetPreview(true, simulatedDmg, simulatedAccDmg);
                    _previewVfx.Add(vfx);
                }
            }
        }

        private void ClearPreviewVfx()
        {
            for (int i = 0; i < _previewVfx.Count; i++)
            {
                var v = _previewVfx[i];
                if (v != null)
                    v.SetPreview(false);
            }
            _previewVfx.Clear();
        }

        private void DisableAllEnemyRangeVfx()
        {
            ClearPreviewVfx();

            if (_battleEnemyManager == null) 
                return;

            var enemies = _battleEnemyManager.GetEnemies();
            if (enemies == null) 
                return;

            foreach (var e in enemies)
            {
                if (e == null) 
                    continue;

                var vfx = e.GetModule<EnemyRangePreviewVfx>();
                if (vfx != null)
                    vfx.SetPreview(false);
            }
        }
        
        public IReadOnlyList<BattleEnemy> GetEnemies() 
        {
            return _battleEnemyManager != null ? _battleEnemyManager.GetEnemies() : null;
        }
        
        private void ShowFirstEnemyInfoOnly(IReadOnlyList<BattleEnemy> enemies)
        {
            if (enemies == null || enemies.Count == 0) return;

            for (int i = 0; i < enemies.Count; i++)
            {
                BattleEnemy enemy = enemies[i];
                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                    continue;

                Bus<EnemyHoverInfoEvent>.Raise(new EnemyHoverInfoEvent(enemy, true));
                return;
            }
        }
        
    }
}