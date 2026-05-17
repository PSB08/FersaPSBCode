using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.UIs
{
    public class BuffListItemView : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Height")]
        [SerializeField] private RectTransform root;

        [SerializeField] private RectTransform headerRoot;
        [SerializeField] private RectTransform descRoot;
        [SerializeField] private LayoutElement rootLayout;

        [SerializeField] private float minHeight = 80f;

        [SerializeField] private bool rebuildNextFrameWhenActive = true;

        private RectTransform _rootRt;
        private bool _pendingRebuild;

        private void Awake()
        {
            _rootRt = root ? root : (RectTransform)transform;

            if (!rootLayout)
                rootLayout = _rootRt.GetComponent<LayoutElement>();
        }

        private void OnEnable()
        {
            if (_pendingRebuild)
            {
                _pendingRebuild = false;
                RebuildHeightNow();
                
                if (rebuildNextFrameWhenActive)
                    StartCoroutine(RebuildNextFrame());
            }
        }

        public void Set(BuffVisualSO dataSo, int remainingTurn)
        {
            if (icon) icon.sprite = dataSo ? dataSo.buffIcon : null;
            if (nameText) nameText.text = dataSo ? dataSo.buffName : "";
            if (turnText) turnText.text = remainingTurn > 0 ? $"남은 턴: {remainingTurn}" : "";
            if (descriptionText) descriptionText.text = dataSo ? dataSo.buffDescription : "";
            
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                _pendingRebuild = true;
                return;
            }
            
            RebuildHeightNow();
            
            if (rebuildNextFrameWhenActive)
                StartCoroutine(RebuildNextFrame());
        }

        private IEnumerator RebuildNextFrame()
        {
            yield return null;
            RebuildHeightNow();
        }

        public void RebuildHeightNow()
        {
            if (_rootRt == null || descriptionText == null)
                return;
            
            descriptionText.ForceMeshUpdate(true);
            
            float descH;
            if (descRoot)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(descRoot);
                descH = LayoutUtility.GetPreferredHeight(descRoot);
            }
            else
            {
                descH = descriptionText.preferredHeight;
            }
            
            float headerH = 0f;
            if (headerRoot)
                headerH = headerRoot.rect.height;

            float target = Mathf.Max(minHeight, (headerH + descH) - 50);
            
            if (rootLayout)
            {
                rootLayout.preferredHeight = target;
            }
            else
            {
                var size = _rootRt.sizeDelta;
                size.y = target;
                _rootRt.sizeDelta = size;
            }
            
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rootRt);

            var parent = _rootRt.parent as RectTransform;
            if (parent)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }
        
    }
}
