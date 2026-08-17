using System.Collections.Generic;
using PSB.Code.BattleCode.Events;
using PSB_Lib.ObjectPool.RunTime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PSB.Code.BattleCode.UIs
{
    public class SystemLogItem : MonoBehaviour, IPoolable, IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private LayoutElement layoutElement;

        [Header("Size")]
        [SerializeField] private float oneLineHeight = 40f;
        [SerializeField] private float additionalLineHeight = 20f;

        [field: SerializeField] public PoolItemSO PoolItem { get; set; }

        private Pool _pool;
        private Dictionary<string, SystemLogLinkData> _linkDataTable;

        public void Init(string message, Color color, Dictionary<string, SystemLogLinkData> linkDataTable = null)
        {
            gameObject.SetActive(true);

            _linkDataTable = linkDataTable;

            if (logText != null)
            {
                logText.richText = true;
                logText.text = message;
                logText.color = color;
                logText.overflowMode = TextOverflowModes.Overflow;
                logText.raycastTarget = true;
            }
        }

        public void RefreshHeight()
        {
            if (logText == null || layoutElement == null) return;

            logText.ForceMeshUpdate(true);

            int lineCount = logText.textInfo.lineCount;
            lineCount = Mathf.Max(1, lineCount);

            float height = oneLineHeight + ((lineCount - 1) * additionalLineHeight);

            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            layoutElement.flexibleHeight = 0f;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (logText == null) return;
            if (_linkDataTable == null) return;

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(
                logText,
                eventData.position,
                eventData.pressEventCamera
            );

            if (linkIndex == -1) return;

            TMP_LinkInfo linkInfo = logText.textInfo.linkInfo[linkIndex];
            string linkId = linkInfo.GetLinkID();

            if (string.IsNullOrEmpty(linkId)) return;
            if (!_linkDataTable.TryGetValue(linkId, out SystemLogLinkData linkData)) return;

            if (SystemLogTooltipUI.Instance != null)
            {
                SystemLogTooltipUI.Instance.Toggle(
                    linkId,
                    linkData.Title,
                    linkData.Description,
                    eventData.position
                );
            }
        }

        public void SetUpPool(Pool pool)
        {
            _pool = pool;
        }

        public void ResetItem()
        {
            _linkDataTable = null;

            if (logText != null)
            {
                logText.text = string.Empty;
                logText.color = Color.white;
            }

            if (layoutElement != null)
            {
                layoutElement.minHeight = oneLineHeight;
                layoutElement.preferredHeight = oneLineHeight;
                layoutElement.flexibleHeight = 0f;
            }

            gameObject.SetActive(false);
        }
        
    }
}