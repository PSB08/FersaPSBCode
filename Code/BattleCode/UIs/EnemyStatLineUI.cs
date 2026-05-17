using PSB_Lib.StatSystem;
using PSW.Code.Battle;
using TMPro;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PSB.Code.BattleCode.UIs
{
    public class EnemyStatLineUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private EventTrigger eventTrigger;

        private RectTransform _rectTransform;
        private EventTrigger.Entry _enter;
        private EventTrigger.Entry _exit;


        public void Set(StatSO stat, int value, StatNamePanel panel)
        {
            if(_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_enter == null)
                _enter = new EventTrigger.Entry();

            if (_exit == null)
                _exit = new EventTrigger.Entry();

            SetEvent(stat, panel);

            if (icon != null)
                icon.sprite = stat.Icon;
            if (text != null)
                text.text = $" : {value}";
        }

        private void SetEvent(StatSO stat, StatNamePanel panel)
        {
            _enter.eventID = EventTriggerType.PointerEnter;
            _enter.callback.AddListener((data) => panel.
            PopUpPanel(_rectTransform.anchoredPosition, stat.displayName));
            eventTrigger.triggers.Add(_enter);

            _exit.eventID = EventTriggerType.PointerExit;
            _exit.callback.AddListener((data) => panel.PopDownPanel());

            eventTrigger.triggers.Add(_exit);
        }
    }

}