using CIW.Code.System.Events;
using DG.Tweening;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.UIs
{
    public class BuffListPopupUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;

        [Header("Panel (Move Target)")]
        [SerializeField] private RectTransform panel;

        [Header("List")]
        [SerializeField] private Transform content;
        [SerializeField] private BuffListItemView itemPrefab;

        [Header("Animation")]
        [SerializeField] private float animDuration = 0.25f;
        [SerializeField] private float offsetY = 250f;

        private Vector2 _originPos;
        private Tween _tween;
        
        private bool _isOpen;
        private ModuleOwner _currentTargetKey;

        private void Awake()
        {
            if (panel)
                _originPos = panel.anchoredPosition;

            if (root)
                root.SetActive(false);

            _isOpen = false;
            _currentTargetKey = null;
        }

        private void OnEnable()
        {
            Bus<BuffListOpenEvent>.OnEvent += OnOpen;
            Bus<BuffListCloseEvent>.OnEvent += OnClose;
            Bus<BattleEnd>.OnEvent += BattleOver;
        }

        private void OnDisable()
        {
            Bus<BuffListOpenEvent>.OnEvent -= OnOpen;
            Bus<BuffListCloseEvent>.OnEvent -= OnClose;
            Bus<BattleEnd>.OnEvent -= BattleOver;
        }

        public void BattleOver(BattleEnd evt)
        {
            if(evt.IsVictory == false)
                OnClose(new());
        }

        private void OnOpen(BuffListOpenEvent evt)
        {
            if (evt.ModuleOwner == null) return;

            if (_isOpen && ReferenceEquals(_currentTargetKey, evt.ModuleOwner))
                return;

            var module = evt.ModuleOwner.GetModule<BuffModule>();
            if (module == null) return;

            Rebuild(module);

            KillTween();

            if (root)
                root.SetActive(true);

            panel.anchoredPosition = _originPos + Vector2.down * offsetY;

            _tween = panel
                .DOAnchorPos(_originPos, animDuration)
                .SetEase(Ease.Linear);

            _isOpen = true;
            _currentTargetKey = evt.ModuleOwner;
        }

        private void OnClose(BuffListCloseEvent evt)
        {
            KillTween();

            _tween = panel
                .DOAnchorPos(_originPos + Vector2.down * offsetY, animDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    if (root)
                        root.SetActive(false);

                    Clear();
                    panel.anchoredPosition = _originPos;

                    _isOpen = false;
                    _currentTargetKey = null;
                });
        }

        private void KillTween()
        {
            if (_tween != null && _tween.IsActive())
                _tween.Kill();
        }

        private void Rebuild(BuffModule module)
        {
            Clear();

            var buffs = module.GetActiveBuffs();
            for (int i = 0; i < buffs.Count; i++)
            {
                var (so, turn) = buffs[i];
                var item = Instantiate(itemPrefab, content);
                item.Set(so, turn);
            }
        }

        private void Clear()
        {
            if (!content) return;

            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
        }

        public void CloseUI()
        {
            Bus<BuffListCloseEvent>.Raise(new BuffListCloseEvent());
        }
        
    }
}
