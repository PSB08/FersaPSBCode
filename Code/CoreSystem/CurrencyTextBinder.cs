using PSB.Code.CoreSystem.Events;
using PSB.Code.CoreSystem.SaveSystem;
using PSW.Code.EventBus;
using TMPro;
using UnityEngine;
using YIS.Code.Defines;

namespace Work.PSB.Code.CoreSystem
{
    public class CurrencyTextBinder : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI bossCoinText;
        [SerializeField] private TextMeshProUGUI ppText;

        [Header("Types")]
        [SerializeField] private ItemType goldType = ItemType.Coin;
        [SerializeField] private ItemType bossCoinType = ItemType.BossCoin;
        [SerializeField] private ItemType ppType = ItemType.PP;
        
        [Header("Format")]
        [SerializeField] private string goldFormat = "Gold: {0}";
        [SerializeField] private string bossCoinFormat = "BossCoin: {0}";
        [SerializeField] private string ppFormat = "PP: {0}";
        
        private void OnEnable()
        {
            Bus<CurrencyChangedEvent>.OnEvent += OnCurrencyChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            Bus<CurrencyChangedEvent>.OnEvent -= OnCurrencyChanged;
        }

        private void OnCurrencyChanged(CurrencyChangedEvent evt)
        {
            if (evt.Type == goldType)
                SetText(goldText, goldFormat, evt.NewValue);
            else if (evt.Type == bossCoinType)
                SetText(bossCoinText, bossCoinFormat, evt.NewValue);
            else if (evt.Type == ppType)
                SetText(ppText, ppFormat, evt.NewValue);
        }

        private void RefreshAll()
        {
            SetText(goldText, goldFormat, CurrencyContainer.Get(goldType));
            SetText(bossCoinText, bossCoinFormat, CurrencyContainer.Get(bossCoinType));
            SetText(ppText, ppFormat, CurrencyContainer.Get(ppType));
        }

        private static void SetText(TextMeshProUGUI text, string format, int value)
        {
            if (!text) return;
            text.text = string.Format(format, value);
        }
        
    }
}