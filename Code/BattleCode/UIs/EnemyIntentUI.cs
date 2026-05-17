using System;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.UI;
using YIS.Code.Defines;

namespace PSB.Code.BattleCode.UIs
{
    public class EnemyIntentUI : MonoBehaviour
    {
        [Header("Owner")]
        [SerializeField] private BattleEnemy owner;

        [Header("UI")]
        [SerializeField] private Image icon;
        [SerializeField] private Image upIcon;

        [Header("Optional")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private EnemyIntentTooltipUI tooltipUI;

        [SerializeField] private Vector2 pos;

        private void Awake()
        {
            if (owner == null)
                owner = GetComponentInParent<BattleEnemy>(true);

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (icon != null)
                icon.enabled = false;
            if (upIcon != null)
                upIcon.enabled = false;
        }

        private void Start()
        {
            Vector3 ownPos = new Vector3(owner.transform.position.x + pos.x, owner.transform.position.y + pos.y, 0f);
            transform.parent.position = ownPos;
        }

        private void OnEnable()
        {
            Bus<EnemyIntentPlannedEvent>.OnEvent += OnPlanned;
            Bus<EnemyIntentClearedEvent>.OnEvent += OnCleared;
        }

        private void OnDisable()
        {
            Bus<EnemyIntentPlannedEvent>.OnEvent -= OnPlanned;
            Bus<EnemyIntentClearedEvent>.OnEvent -= OnCleared;
        }

        private void OnPlanned(EnemyIntentPlannedEvent evt)
        {
            if (owner == null) return;
            if (evt.enemy != owner) return;

            Sprite sprite = ResolveIcon(evt);
            Set(sprite);
            
            if (tooltipUI != null)
            {
                tooltipUI.Set(owner, evt.skillSo);
            }
        }

        private void OnCleared(EnemyIntentClearedEvent evt)
        {
            if (owner == null) return;
            if (evt.enemy != owner) return;

            Hide();
        }

        private Sprite ResolveIcon(EnemyIntentPlannedEvent evt)
        {
            if (evt.skillSo.grade >= Grade.Epic)
                upIcon.enabled = true;
            else
                upIcon.enabled = false;
            
            if (evt.skillSo != null && evt.skillSo.visualData != null)
                return evt.skillSo.visualData.icon;

            return null;
        }

        public void Set(Sprite sprite)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
            else
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (icon != null)
                icon.enabled = false;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            else
                gameObject.SetActive(false);
        }
        
    }
}