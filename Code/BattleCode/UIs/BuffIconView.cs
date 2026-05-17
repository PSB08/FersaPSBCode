using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.UIs
{
    public class BuffIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text durationText;

        [Header("Tooltip")]
        [SerializeField] private BuffTooltipUI tooltipPrefab;

        private BuffVisualSO _buffVisualData;
        private BuffTooltipUI _tooltipInstance;
        private BuffModule _ownerBuffModule;
        private int _duration;

        public void Set(BuffVisualSO buffData, int duration, ModuleOwner owner, bool isLeft)
        {
            _buffVisualData = buffData;
            _duration = duration;
            _ownerBuffModule = owner != null ? owner.GetModule<BuffModule>() : null;

            if (icon != null)
                icon.sprite = buffData != null ? buffData.buffIcon : null;

            if (durationText != null)
                durationText.text = duration > 0 ? duration.ToString() : "";
        }

        private void OnDisable()
        {
            DestroyTooltip();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_buffVisualData == null || tooltipPrefab == null) return;
            if (_tooltipInstance != null) return;

            var rootGo = GameObject.Find("TooltipRoot");
            if (!rootGo)
                return;

            var rootRt = rootGo.transform as RectTransform;
            if (!rootRt)
                return;

            _tooltipInstance = Instantiate(tooltipPrefab, rootRt);
            _tooltipInstance.Set(_buffVisualData, _duration);

            var cg = _tooltipInstance.GetComponent<CanvasGroup>();
            if (!cg) 
                cg = _tooltipInstance.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var tipRt = _tooltipInstance.GetComponent<RectTransform>();
            
            tipRt.anchorMin = new Vector2(0.5f, 0.5f);
            tipRt.anchorMax = new Vector2(0.5f, 0.5f);
            tipRt.pivot = new Vector2(0.5f, 0.5f);
            
            tipRt.anchoredPosition = Vector2.zero;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            DestroyTooltip();
        }

        private void DestroyTooltip()
        {
            if (_tooltipInstance != null)
            {
                Destroy(_tooltipInstance.gameObject);
                _tooltipInstance = null;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_ownerBuffModule == null) return;

            Bus<BuffListOpenEvent>.Raise(new BuffListOpenEvent(_ownerBuffModule.UiTarget));
        }
        
    }
}