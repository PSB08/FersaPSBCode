using PSB.Code.FieldCode.BTs.Events;
using PSW.Code.EventBus;
using TMPro;
using UnityEngine;

namespace Work.PSB.Code.FieldCode.UIs
{
    public class EnemyCountUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI countText;

        private void OnEnable()
        {
            Bus<EnemyCountUpdateEvent>.OnEvent += UpdateText;
        }

        private void OnDisable()
        {
            Bus<EnemyCountUpdateEvent>.OnEvent -= UpdateText;
        }

        private void UpdateText(EnemyCountUpdateEvent evt)
        {
            if (countText != null)
            {
                var s = evt.TotalCount != 0 ? $"남은 적 : {evt.AliveCount} / {evt.TotalCount}" : string.Empty;
                countText.text = s;
            }
        }
        
    }
}