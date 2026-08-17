using System.Collections.Generic;
using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace PSB.Code.BattleCode.UIs
{
    public class SystemLogUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform contentRoot;
        [SerializeField] private PoolItemSO logItemPrefab;
        [SerializeField] private int maxLogCount = 50;
        [SerializeField] private ScrollRect scrollRect;

        [Header("Colors")]
        [SerializeField] private Color playerColor = new Color(0.3f, 0.55f, 1f, 1f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color neutralColor = Color.white;

        [Inject] private PoolManagerMono _poolManager;

        private readonly Queue<SystemLogItem> _activeLogs = new Queue<SystemLogItem>();

        private void Awake()
        {
            if (Injector.Instance != null)
            {
                Injector.Instance.InjectTo(this);
            }
        }

        private void OnEnable()
        {
            Bus<SystemLogEvent>.OnEvent += OnSystemLog;
        }

        private void OnDisable()
        {
            Bus<SystemLogEvent>.OnEvent -= OnSystemLog;
        }

        private void OnDestroy()
        {
            ClearAllLogs();
        }

        private void OnSystemLog(SystemLogEvent evt)
        {
            if (_poolManager == null || logItemPrefab == null || contentRoot == null) return;
            if (string.IsNullOrEmpty(evt.Message)) return;

            SystemLogItem item = _poolManager.Pop<SystemLogItem>(logItemPrefab);

            if (item == null) return;

            item.transform.SetParent(contentRoot, false);
            item.transform.localScale = Vector3.one;
            item.transform.SetAsLastSibling();

            item.Init(evt.Message, GetColor(evt.Owner), evt.LinkDataTable);

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentRoot);
            Canvas.ForceUpdateCanvases();

            item.RefreshHeight();

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentRoot);
            Canvas.ForceUpdateCanvases();

            _activeLogs.Enqueue(item);

            if (_activeLogs.Count > maxLogCount)
            {
                SystemLogItem oldItem = _activeLogs.Dequeue();

                if (oldItem != null)
                {
                    _poolManager.Push(oldItem);
                }
            }

            UpdateScrollToBottom();
        }

        private Color GetColor(SystemLogOwner owner)
        {
            switch (owner)
            {
                case SystemLogOwner.Player:
                    return playerColor;

                case SystemLogOwner.Enemy:
                    return enemyColor;

                case SystemLogOwner.Neutral:
                    return neutralColor;

                default:
                    return neutralColor;
            }
        }

        private void UpdateScrollToBottom()
        {
            if (scrollRect == null) return;

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        private void ClearAllLogs()
        {
            if (_poolManager == null) return;

            while (_activeLogs.Count > 0)
            {
                SystemLogItem item = _activeLogs.Dequeue();

                if (item != null)
                {
                    _poolManager.Push(item);
                }
            }
        }
        
    }
}