using PSB.Code.BattleCode.Enemies;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.UIs
{
    public class EnemyIntentTooltipUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
       [SerializeField] private Image icon;

        [Header("Tooltip")]
        [SerializeField] private EnemySkillTooltipUI tooltipPrefab;
        [SerializeField] private Vector2 screenPadding = new Vector2(8f, 8f);

        [SerializeField] private float spawnOffsetX = 250f;
        [SerializeField] private float spawnOffsetY = 50f;
        [SerializeField] private RectTransform anchorTarget;
        
        private SkillDataSO _skill;
        private BattleEnemy _enemy;
        private EnemySkillTooltipUI _tooltipInstance;
        private bool _isLeft;
        
        public void Set(BattleEnemy enemy, SkillDataSO skill, bool isLeft = false)
        {
            _enemy = enemy;
            _skill = skill;
            _isLeft = isLeft;

            if (icon != null)
                icon.sprite = skill != null && skill.visualData != null ? skill.visualData.icon : null;
        }

        private void OnDisable()
        {
            DestroyTooltip();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_skill == null || tooltipPrefab == null) return;
            if (_tooltipInstance != null) return;

            var rootGo = GameObject.Find("TooltipRoot2");
            if (!rootGo)
                return;

            var root = rootGo.transform as RectTransform;
            if (!root)
                return;

            var canvas = rootGo.GetComponentInParent<Canvas>();
            Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) 
                ? canvas.worldCamera : null;
            
            _tooltipInstance = Instantiate(tooltipPrefab, root);
            _tooltipInstance.Set(_enemy, _skill);

            var cg = _tooltipInstance.GetComponent<CanvasGroup>();
            if (!cg) 
                cg = _tooltipInstance.gameObject.AddComponent<CanvasGroup>();
            
            cg.blocksRaycasts = false;
            cg.interactable = false;

            var tipRt = _tooltipInstance.GetComponent<RectTransform>();
            
            tipRt.anchorMin = tipRt.anchorMax = new Vector2(0.5f, 0.5f);
            tipRt.pivot = new Vector2(0.5f, 0f);
            
            var target = anchorTarget != null
                ? anchorTarget
                : (icon != null ? icon.rectTransform : null);

            if (target == null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, eventData.position, 
                    uiCam, out Vector2 mouseLocal);
                float offset1 = _isLeft ? -spawnOffsetX + 125f : spawnOffsetX;
                Vector2 fallbackPos = mouseLocal + new Vector2(offset1, spawnOffsetY);
                
                fallbackPos = ClampX(root, tipRt, fallbackPos);
                tipRt.anchoredPosition = fallbackPos;
                return;
            }

            var rootCanvas = root.GetComponentInParent<Canvas>();
            Camera rootCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) 
                ? rootCanvas.worldCamera : null;

            if (rootCanvas != null && rootCanvas.renderMode 
                != RenderMode.ScreenSpaceOverlay && rootCam == null)
                rootCam = Camera.main;

            Vector2 iconTopCenterLocal = GetTopCenterLocalInRoot(root, target, rootCam, eventData);

            float offset2 = _isLeft ? -spawnOffsetX + 125f : spawnOffsetX;
            Vector2 pos = iconTopCenterLocal + new Vector2(offset2, spawnOffsetY);

            pos = ClampX(root, tipRt, pos);

            tipRt.anchoredPosition = pos;
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

        private Vector2 GetTopCenterLocalInRoot(RectTransform root, RectTransform target, 
            Camera rootCam, PointerEventData eventData)
        {
            var targetCanvas = target.GetComponentInParent<Canvas>();

            Camera targetCam = null;
            if (targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                targetCam = targetCanvas.worldCamera;
            
            if (targetCam == null)
                targetCam = eventData != null ? eventData.pressEventCamera : null;
            if (targetCam == null)
                targetCam = Camera.main;
            
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            
            Vector3 topCenterWorld = (corners[1] + corners[2]) * 0.5f;
            
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(targetCam, topCenterWorld);
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, rootCam, out Vector2 local);
            return local;
        }

        private Vector2 ClampX(RectTransform root, RectTransform tipRt, Vector2 desiredPos)
        {
            Rect r = root.rect;
            Vector2 size = tipRt.rect.size;

            float minX = r.xMin + screenPadding.x + size.x * tipRt.pivot.x;
            float maxX = r.xMax - screenPadding.x - size.x * (1f - tipRt.pivot.x);

            desiredPos.x = Mathf.Clamp(desiredPos.x, minX, maxX);
            return desiredPos;
        }
            
    }
}