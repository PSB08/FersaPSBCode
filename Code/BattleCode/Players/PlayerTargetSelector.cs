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
                _currentIndex = -1;
                _currentTarget = null;
                ClearHighlight();
                DisableAllEnemyRangeVfx();
                return;
            }

            _currentIndex = 0;
            _currentTarget = enemies[_currentIndex];
            HighlightEnemy(_currentTarget);
        }

        private void TrySelectAndHighlightFirstEnemy()
        {
            var enemies = _battleEnemyManager != null ? _battleEnemyManager.GetEnemies() : null;
            if (enemies == null || enemies.Count == 0)
            {
                _currentIndex = -1;
                _currentTarget = null;
                ClearHighlight();
                DisableAllEnemyRangeVfx();
                return;
            }

            if (_currentIndex < 0) _currentIndex = 0;
            _currentIndex = Mathf.Clamp(_currentIndex, 0, enemies.Count - 1);

            if (_currentTarget != null)
            {
                int idx = IndexOf(enemies, _currentTarget);
                if (idx >= 0)
                {
                    _currentIndex = idx;
                    HighlightEnemy(_currentTarget);
                    return;
                }
            }

            _currentTarget = enemies[_currentIndex];
            HighlightEnemy(_currentTarget);
        }

        public void MoveUp()
        {
            if (_inputLocked) return;

            var enemies = _battleEnemyManager != null ? _battleEnemyManager.GetEnemies() : null;
            if (enemies == null || enemies.Count <= 1) return;

            if (_currentIndex < 0) _currentIndex = 0;

            _currentIndex = (_currentIndex + 1) % enemies.Count;
            _currentTarget = enemies[_currentIndex];

            HighlightEnemy(_currentTarget);
            RefreshPreviewVfx();
        }

        public void MoveDown()
        {
            if (_inputLocked) return;

            var enemies = _battleEnemyManager != null ? _battleEnemyManager.GetEnemies() : null;
            if (enemies == null || enemies.Count <= 1) 
                return;

            if (_currentIndex < 0) _currentIndex = 0;

            _currentIndex = (_currentIndex - 1 + enemies.Count) % enemies.Count;
            _currentTarget = enemies[_currentIndex];

            HighlightEnemy(_currentTarget);
            RefreshPreviewVfx();
        }

        private void HighlightEnemy(BattleEnemy target)
        {
            if (target == null)
                return;

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
                _currentTarget = null;
                _currentIndex = -1;
                return null;
            }

            if (_currentTarget != null && !_currentTarget.IsDead)
            {
                int idx = IndexOf(enemies, _currentTarget);
                if (idx >= 0)
                    return _currentTarget;
            }

            if (_currentIndex < 0) _currentIndex = 0;
            int startIdx = _currentIndex;
            int attempt = 0;

            while (attempt < enemies.Count)
            {
                int checkIdx = (startIdx + attempt) % enemies.Count;
                var candidate = enemies[checkIdx];

                if (candidate != null && !candidate.IsDead)
                {
                    _currentIndex = checkIdx;
                    _currentTarget = candidate;

                    HighlightEnemy(_currentTarget);
                    return _currentTarget;
                }

                attempt++;
            }

            _currentTarget = null;
            _currentIndex = -1;
            ClearHighlight();
            return null;

            //_currentIndex = Mathf.Clamp(_currentIndex, 0, enemies.Count - 1);

            //_currentTarget = enemies[_currentIndex];
            //return _currentTarget;
        }

        public int GetCurrentTargetIndex()
        {
            var enemies = _battleEnemyManager != null ? _battleEnemyManager.GetEnemies() : null;
            if (enemies == null || enemies.Count == 0) 
                return -1;

            if (_currentTarget != null)
            {
                int idx = IndexOf(enemies, _currentTarget);
                if (idx >= 0) 
                    return idx;
            }

            return _currentIndex;
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

            foreach (var enemy in enemies)
            {
                var vfx = enemy.GetModule<EnemyRangePreviewVfx>();
                if (vfx == null) continue;

                bool[] isHitBySlot = null;
                bool isHitByAnything = false;

                if (_currentPreviewSkills != null && _currentPreviewSetIndex != null)
                {
                    isHitBySlot = new bool[_currentPreviewSkills.Length];
                    for (int i = 0; i < _currentPreviewSkills.Length; i++)
                    {
                        if (_currentPreviewSkills[i] != null && _currentPreviewSetIndex[i])
                        {
                            var targetsForSkill = SkillTargetingUtil.GetTargetsByRange(enemies, centerIndex, _currentPreviewSkills[i].range);
                            if (targetsForSkill != null && targetsForSkill.Contains(enemy))
                            {
                                isHitBySlot[i] = true;
                                isHitByAnything = true;
                            }
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
        
    }
}