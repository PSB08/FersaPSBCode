using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YIS.Code.Items;

namespace PSB.Code.BattleCode.UIs
{
    public class VictoryLootEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private ItemPanelTooltipUI tooltipUI;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI amountText;

        private void Start()
        {
            tooltipUI.gameObject.SetActive(false);
        }

        public void Bind(ItemDataSO item, int amount)
        {
            var visual = item != null ? item.visualData : null;

            tooltipUI.Bind(item);
            
            if (icon != null)
            {
                icon.enabled = visual != null && visual.icon != null;
                icon.sprite = visual != null ? visual.icon : null;
            }

            if (amountText != null)
                amountText.SetText(Mathf.Max(1, amount).ToString());
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            tooltipUI.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltipUI.gameObject.SetActive(false);
        }
        
    }
}