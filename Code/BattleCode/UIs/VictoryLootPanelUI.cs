using System.Collections;
using System.Collections.Generic;
using PSB.Code.BattleCode.BattleSystems;
using UnityEngine;
using Work.PSB.Code.CoreSystem;
using Work.PSB.Code.CoreSystem.Tests;
using YIS.Code.Defines;
using YIS.Code.Items;

namespace PSB.Code.BattleCode.UIs
{
    public class VictoryLootPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform contentParent;
        [SerializeField] private VictoryLootEntryUI entryPrefab;
        [SerializeField] private TransitionController controller;
        
        [Header("Currency Texts")]
        [SerializeField] private ItemDataSO coinItem;
        [SerializeField] private ItemDataSO bossCoinItem;
        [SerializeField] private ItemDataSO ppItem;

        [Header("Size Animation")]
        [SerializeField] private EndPanelSizeToggle_Model sizeModel;
        [SerializeField] private EndPanelSizeToggle_View sizeView;

        private bool _isShown;

        private void Awake()
        {
            sizeView.Init(sizeModel.InitModel());
            _isShown = false;
        }

        private void Show()
        {
            if (_isShown) return;
            _isShown = true;

            sizeView.AnimateTo(
                sizeModel.GetShowSize(),
                sizeModel.GetPopTime()
            );

            Rebuild();
        }
        
        public IEnumerator ShowCoroutine()
        {
            yield return new WaitForSeconds(0.5f);
            Show();
        }

        public void Hide()
        {
            if (!_isShown) return;
            _isShown = false;
            Time.timeScale = 1;

            sizeView.AnimateTo(
                sizeModel.GetHideSize(),
                0.25f
            );
        }
        
        private void Rebuild()
        {
            ClearChildren();

            var session = BattleLootSession.Instance;
            if (session == null) return;
            
            var currencies = session.CurrencySnapshot();

            AddCurrencyEntry(currencies, ItemType.Coin, coinItem);
            AddCurrencyEntry(currencies, ItemType.BossCoin, bossCoinItem);
            AddCurrencyEntry(currencies, ItemType.PP, ppItem);
            
            var items = session.ItemSnapshot();
            foreach (var kv in items)
            {
                if (kv.Key == null || kv.Value <= 0) continue;

                Instantiate(entryPrefab, contentParent)
                    .Bind(kv.Key, kv.Value);
            }
        }

        private void AddCurrencyEntry(Dictionary<ItemType, int> map, ItemType type, ItemDataSO itemData)
        {
            if (map == null) return;
            if (itemData == null) return;

            if (!map.TryGetValue(type, out int amount) || amount <= 0)
                return;

            Instantiate(entryPrefab, contentParent)
                .Bind(itemData, amount);
        }

        private void ClearChildren()
        {
            if (contentParent == null) return;

            for (int i = contentParent.childCount - 1; i >= 0; i--)
                Destroy(contentParent.GetChild(i).gameObject);
        }

        public void ExitBtn()
        {
            KillCounter.Instance?.CommitKills();
            BattleLootSession.Instance?.Clear();
            Hide();
            Invoke(nameof(DoTransition), sizeModel.GetPopTime());
        }

        private void DoTransition()
        {
            controller.Transition();
        }
        
        public void SetReturnScene(string sceneName)
        {
            controller.nextScene = sceneName;
        }
        
    }
}