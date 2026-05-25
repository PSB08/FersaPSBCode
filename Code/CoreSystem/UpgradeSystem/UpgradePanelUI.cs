using Code.Scripts.Entities;
using DG.Tweening;
using PSB.Code.CoreSystem.Events;
using PSB.Code.CoreSystem.SaveSystem;
using PSW.Code.BaseSystem;
using PSW.Code.EventBus;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIS.Code.Defines;

namespace Work.PSB.Code.CoreSystem.UpgradeSystem
{
    public class UpgradePanelUI : MonoBehaviour, IBaseSystemUI
    {
        [Serializable]
        public class MilestoneUI
        {
            public int targetLevel;
            public Image icon;
            public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            public Color unlockedColor = Color.white;
        }

        [Serializable]
        public class Entry
        {
            public Button button;
            public UpgradeDefSO def;

            [Header("UI - Display")]
            public TextMeshProUGUI enhancedValueText;
            
            public string enhancedFormat = "{0} + {1}";

            [Header("UI - Level")]
            public TextMeshProUGUI levelText;
            public string levelFormat = "Lv.{0}";

            [Header("UI - PP")]
            public TextMeshProUGUI ppText;
            public string ppFormat = "PP : {0}";

            [Header("Milestones")]
            public MilestoneUI[] milestoneIcons;
        }

        [Header("References")]
        [SerializeField] private EntityStat playerStat;

        [Header("Entries")]
        [SerializeField] private Entry[] entries;

        [Header("Top Info")]
        [SerializeField] private TextMeshProUGUI ppValueTxt;
        [SerializeField] private TextMeshProUGUI progressLevelTxt;
        
        [SerializeField] private string progressLevelFormat = "진행한 강화 횟수: {0} / {1}";

        [Header("Reset")]
        [SerializeField] private Button resetBtn;
        [SerializeField] private ItemType resetCostType = ItemType.PP;
        [SerializeField] private ItemType pointValueType = ItemType.PP;
        [SerializeField] private int resetCost = 500;

        [Header("Settings")]
        [SerializeField] private bool disableButton = true;
        [SerializeField] private int globalMaxLevel = 30;

        private UpgradeService _service;
        private bool _isOpened;

        private void Awake()
        {
            Debug.Log("서비스 생성");
            _service = new UpgradeService(playerStat, globalMaxLevel);
        }

        private void OnEnable()
        {
            Bus<CurrencyChangedEvent>.OnEvent += OnCurrencyChanged;

            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (e?.button == null) continue;

                    Entry entry = e;
                    e.button.onClick.AddListener(() =>
                    {
                        if (_service.TryUpgrade(entry.def, GetAllDefs()))
                        {
                            RefreshAll();
                        }
                    });
                }
            }

            if (resetBtn != null)
            {
                resetBtn.onClick.AddListener(() =>
                {
                    if (_service.GetTotalGlobalLevel(GetAllDefs()) <= 0) return;

                    if (_service.TryResetAllUpgrades(GetAllDefs(), resetCostType, resetCost))
                    {
                        RefreshAll();
                    }
                });
            }
        }

        private void Start()
        {
            RefreshAll();
        }

        private void OnDisable()
        {
            Bus<CurrencyChangedEvent>.OnEvent -= OnCurrencyChanged;

            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (e?.button != null) e.button.onClick.RemoveAllListeners();
                }
            }

            if (resetBtn != null) resetBtn.onClick.RemoveAllListeners();
        }

        private void OnCurrencyChanged(CurrencyChangedEvent evt)
        {
            if (evt.Type == pointValueType || evt.Type == resetCostType) 
                RefreshAll();
        }

        public void RefreshAll()
        {
            if (ppValueTxt != null)
                ppValueTxt.text = CurrencyContainer.Get(pointValueType).ToString();

            if (progressLevelTxt != null)
            {
                progressLevelTxt.text = string.Format(progressLevelFormat, 
                    _service.GetTotalGlobalLevel(GetAllDefs()), globalMaxLevel);
            }

            if (resetBtn != null)
            {
                bool canAffordReset = CurrencyContainer.Get(resetCostType) >= resetCost;
                bool hasAnyUpgrade = _service.GetTotalGlobalLevel(GetAllDefs()) > 0;
                
                resetBtn.interactable = canAffordReset && hasAnyUpgrade;
            }

            if (entries == null) return;

            foreach (var e in entries)
            {
                RefreshEntry(e);
                RefreshInteractable(e);
                RefreshMilestoneIcons(e);
            }
        }

        private void RefreshEntry(Entry e)
        {
            if (e == null || e.def == null || e.def.targetStat == null)
            {
                if (e?.enhancedValueText != null) e.enhancedValueText.text = "-";
                if (e?.levelText != null) e.levelText.text = "-";
                if (e?.ppText != null) e.ppText.text = "-";
                return;
            }

            if (playerStat == null)
            {
                if (e.enhancedValueText != null) e.enhancedValueText.text = "?";
                if (e.levelText != null) e.levelText.text = "?";
                if (e.ppText != null) e.ppText.text = "-";
                return;
            }

            int current = (int)playerStat.GetBaseValue(e.def.targetStat);
            int baseValue = e.def.baseReferenceValue;
            int enhanced = current - baseValue;

            int level = _service.GetCurrentLevel(e.def);
            int pp = e.def.GetNextCost(level);

            if (e.enhancedValueText != null)
            {
                e.enhancedValueText.text = string.Format(e.enhancedFormat, e.def.targetStat.displayName, enhanced);
            }

            if (e.levelText != null)
            {
                e.levelText.text = string.Format(e.levelFormat, level);
            }

            if (e.ppText != null)
            {
                e.ppText.text = string.Format(e.ppFormat, pp);
            }
        }

        private void RefreshInteractable(Entry e)
        {
            if (e == null || e.def == null || e.button == null) return;

            int level = _service.GetCurrentLevel(e.def);
            int globalLevel = _service.GetTotalGlobalLevel(GetAllDefs());

            bool isMaxOrGlobalMax = e.def.IsMax(level) || globalLevel >= globalMaxLevel;

            if (isMaxOrGlobalMax)
            {
                e.button.interactable = false;
            }
            else
            {
                int cost = e.def.GetNextCost(level);
                e.button.interactable = !disableButton 
                                        || CurrencyContainer.Get(e.def.costType) >= cost;
            }
        }

        private void RefreshMilestoneIcons(Entry e)
        {
            if (e == null || e.def == null || e.milestoneIcons == null) return;

            int level = _service.GetCurrentLevel(e.def);

            foreach (var m in e.milestoneIcons)
            {
                if (m.icon == null) continue;
                m.icon.color = (level >= m.targetLevel) ? m.unlockedColor : m.lockedColor;
            }
        }

        public UpgradeDefSO[] GetAllDefs()
        {
            if (entries == null) return null;
            UpgradeDefSO[] defs = new UpgradeDefSO[entries.Length];
            for (int i = 0; i < entries.Length; i++)
                defs[i] = entries[i]?.def;
            return defs;
        }

        public void DataInit()
        {
        }

        public void DataDestroy()
        {
            RefreshAll();
        }
        
    }
}