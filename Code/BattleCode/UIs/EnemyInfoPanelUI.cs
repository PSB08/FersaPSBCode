using System.Collections.Generic;
using Code.Scripts.Entities;
using PSB_Lib.StatSystem;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Enemies.AttackCode;
using PSB.Code.BattleCode.Enemies.BTs.Events;
using PSB.Code.BattleCode.Entities;
using PSB.Code.BattleCode.Events;
using PSW.Code.Battle;
using PSW.Code.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIS.Code.Modules;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.UIs
{
    public class EnemyInfoPanelUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Enemy Basic Info")]
        [SerializeField] private Image enemyIcon;
        [SerializeField] private TextMeshProUGUI enemyName;

        [Header("HP UI")]
        [SerializeField] private HpUI_Controller hpController;

        [Header("Stats")]
        [SerializeField] private Transform statRoot;
        [SerializeField] private StatNamePanel statNamePanel;
        [SerializeField] private EnemyStatLineUI statLinePrefab;

        [Header("Skills")]
        [SerializeField] private Transform skillRoot;
        [SerializeField] private EnemySkillItemUI skillItemPrefab;
        [SerializeField] private RectTransform tooltipAnchor;

        [Header("Buffs")]
        [SerializeField] private Transform buffContent;
        [SerializeField] private BuffIconView buffIconPrefab;
        [SerializeField] private TextMeshProUGUI emptyText;

        private BattleEnemy _currentEnemy;
        private EntityHealth _currentHealth;
        private BuffModule _currentBuffModule;

        private ModuleOwner _currentBuffEventTarget;

        private readonly List<GameObject> _spawnedStats = new();
        private readonly List<GameObject> _spawnedSkills = new();
        
        private readonly List<BuffIconView> _spawnedBuffIcons = new();

        private void Awake()
        {
            if (root != null)
                root.SetActive(false);
            
            UpdateEmptyTextVisibility();
        }

        private void OnEnable()
        {
            Bus<EnemyHoverInfoEvent>.OnEvent += HandleEnemyInfoShow;
            Bus<EnemyInfoCloseEvent>.OnEvent += HandleClose;
            Bus<BuffUiEvent>.OnEvent += HandleBuffUiEvent;
            Bus<EnemySkillsChangedEvent>.OnEvent += HandleSkillsChanged;
        }

        private void OnDisable()
        {
            Bus<EnemyHoverInfoEvent>.OnEvent -= HandleEnemyInfoShow;
            Bus<EnemyInfoCloseEvent>.OnEvent -= HandleClose;
            Bus<BuffUiEvent>.OnEvent -= HandleBuffUiEvent;
            Bus<EnemySkillsChangedEvent>.OnEvent -= HandleSkillsChanged;

            Close();
        }

        private void HandleEnemyInfoShow(EnemyHoverInfoEvent evt)
        {
            if (!evt.Show) return;
            if (evt.Enemy == null) return;

            _currentEnemy = evt.Enemy;

            if (root != null)
                root.SetActive(true);

            Apply(_currentEnemy);
        }
        
        private void HandleClose(EnemyInfoCloseEvent evt) => Close();
        
        private void Close()
        {
            UnbindHealth();

            _currentEnemy = null;
            _currentBuffModule = null;
            _currentBuffEventTarget = null;

            ClearSpawnedStats();
            ClearSpawnedSkills();
            ClearBuffViews();

            if (root != null)
                root.SetActive(false);
        }

        private void Apply(BattleEnemy enemy)
        {
            ClearSpawnedStats();
            ClearSpawnedSkills();
            ClearBuffViews();
            UnbindHealth();

            _currentBuffModule = null;
            _currentBuffEventTarget = null;

            if (enemy == null) return;

            EnemySO so = enemy.enemySO;
            if (so == null) return;
            
            if (enemyIcon != null) enemyIcon.sprite = so.icon;
            if (enemyName != null) enemyName.text = so.enemyName;
            
            _currentHealth = enemy.GetModule<EntityHealth>();
            if (_currentHealth != null && hpController != null)
            {
                _currentHealth.OnHealthChangeEvent += HandleHealthChanged;
                _currentHealth.OnShieldChangeEvent += HandleShieldChanged;
                hpController.Init(_currentHealth.CurrentHealth, _currentHealth.MaxHealth,
                    _currentHealth.CurrentShield, isLeft: false);
            }
            
            var statComp = enemy.GetModule<EntityStat>();
            if (statComp != null)
            {
                foreach (var stat in statComp.GetAllStats())
                {
                    if (stat == null) continue;
                    if (stat.statName == "HP") continue;

                    int value = Mathf.RoundToInt(stat.Value);
                    SpawnStatLine(stat, value);
                }
            }

            SkillDataSO[] currentSkills = so.attackSkills;
            var enemyAttack = enemy.GetModule<EnemyAttack>();
            if (enemyAttack != null && enemyAttack.CurrentSkills != null)
            {
                currentSkills = enemyAttack.CurrentSkills;
            }
            
            UpdateSkillsUI(currentSkills);
            
            _currentBuffModule = enemy.GetComponentInChildren<BuffModule>(true);
            if (_currentBuffModule != null)
            {
                _currentBuffEventTarget = _currentBuffModule.UiTarget;
				
                if (_currentBuffEventTarget == null)
                    _currentBuffEventTarget = enemy;
				
                SyncBuffs(_currentBuffEventTarget);
            }
        }

        private void HandleSkillsChanged(EnemySkillsChangedEvent evt)
        {
            if (_currentEnemy != null && _currentEnemy == evt.Enemy)
            {
                UpdateSkillsUI(evt.NewSkills);
            }
        }

        private void UpdateSkillsUI(SkillDataSO[] skills)
        {
            ClearSpawnedSkills();
            
            if (skills == null || skillItemPrefab == null || skillRoot == null) return;

            for (int i = 0; i < skills.Length; i++)
            {
                SkillDataSO skill = skills[i];
                if (skill == null) continue;

                var item = Instantiate(skillItemPrefab, skillRoot);
                _spawnedSkills.Add(item.gameObject);

                item.Set(_currentEnemy, skill, tooltipAnchor);
            }
        }

        private void HandleBuffUiEvent(BuffUiEvent evt)
        {
            if (_currentEnemy == null) return;
            if (_currentBuffModule == null) return;
            if (_currentBuffEventTarget == null) return;
            if (evt.Target == null) return;
            
            if (!ReferenceEquals(evt.Target, _currentBuffEventTarget))
                return;
            
            SyncBuffs(evt.Target);
        }

        private void SyncBuffs(ModuleOwner target)
        {
            ClearBuffViews();

            BuffModule buffModule = target.GetModule<BuffModule>();
            if (buffModule == null || buffContent == null || buffIconPrefab == null) return;

            var activeBuffs = buffModule.GetActiveBuffs();
            
            Dictionary<BuffVisualSO, int> groupedBuffs = new Dictionary<BuffVisualSO, int>();

            foreach (var buff in activeBuffs)
            {
                if (buff.so == null) continue;

                if (groupedBuffs.TryGetValue(buff.so, out int currentMaxTurn))
                {
                    groupedBuffs[buff.so] = Mathf.Max(currentMaxTurn, buff.remainingTurn);
                }
                else
                {
                    groupedBuffs[buff.so] = buff.remainingTurn;
                }
            }

            foreach (var kvp in groupedBuffs)
            {
                var so = kvp.Key;
                var maxTurn = kvp.Value;

                var view = Instantiate(buffIconPrefab, buffContent);
                view.Set(so, maxTurn, target, isLeft: false);
                
                _spawnedBuffIcons.Add(view);
            }

            UpdateEmptyTextVisibility();
        }

        private void ClearBuffViews()
        {
            foreach (var view in _spawnedBuffIcons)
            {
                if (view != null) Destroy(view.gameObject);
            }

            _spawnedBuffIcons.Clear();
            UpdateEmptyTextVisibility();
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (hpController == null) return;
            hpController.ChangeMaxHp(max);
            hpController.ChangeCurrentHp(current);
        }
        
        private void HandleShieldChanged(float shield)
        {
            if (hpController == null) return;
            hpController.ChangeShield(shield);
        }
        
        private void UnbindHealth()
        {
            if (_currentHealth != null)
            {
                _currentHealth.OnHealthChangeEvent -= HandleHealthChanged;
                _currentHealth.OnShieldChangeEvent -= HandleShieldChanged;
            }
            
            _currentHealth = null;
        }
        
        private void SpawnStatLine(StatSO stat, int value)
        {
            if (statLinePrefab == null || statRoot == null) return;
            
            var line = Instantiate(statLinePrefab, statRoot);
            _spawnedStats.Add(line.gameObject);
            
            line.Set(stat, value, statNamePanel);
        }
        
        private void ClearSpawnedStats()
        {
            for (int i = 0; i < _spawnedStats.Count; i++)
                if (_spawnedStats[i] != null) Destroy(_spawnedStats[i]);

            _spawnedStats.Clear();
        }
        
        private void ClearSpawnedSkills()
        {
            for (int i = 0; i < _spawnedSkills.Count; i++)
                if (_spawnedSkills[i] != null) Destroy(_spawnedSkills[i]);

            _spawnedSkills.Clear();
        }
        
        private void UpdateEmptyTextVisibility()
        {
            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(_spawnedBuffIcons.Count == 0);
            }
        }
        
    }

}
