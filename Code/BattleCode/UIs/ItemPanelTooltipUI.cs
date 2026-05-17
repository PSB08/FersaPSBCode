using TMPro;
using UnityEngine;
using YIS.Code.Items;

namespace PSB.Code.BattleCode.UIs
{
    public class ItemPanelTooltipUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI amountText;

        public void Bind(ItemDataSO item)
        {
            var visual = item != null ? item.visualData : null;

            if (amountText != null)
            {
                if (visual != null)
                    amountText.text = visual.uiName;
            }
        }
        
    }
}