using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PSB.Code.BattleCode.UIs
{
    public class TutorialAutoScroll : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IScrollHandler
    {
        [Header("Scroll refs")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewportRt;
        [SerializeField] private RectTransform contentRt;

        [Header("Auto Scroll")]
        [SerializeField] private bool autoScroll = true;
        [SerializeField] private float startDelay = 0.15f;
        [SerializeField] private float scrollSpeed = 35f;
        [SerializeField] private float endDelay = 0.2f;
        [SerializeField] private bool loop = false;

        private Coroutine _co;
        private bool _isDragging;

        public void OnEnable()
        {
            Canvas.ForceUpdateCanvases();
            if (contentRt) 
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            Canvas.ForceUpdateCanvases();
            
            ResetScrollTop();
            
            if (autoScroll) StartAutoScrollIfNeeded();
        }
        
        private void OnDisable() => StopAutoScroll();
        
        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            StopAutoScroll(); 
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            if (autoScroll) StartAutoScrollIfNeeded();
        }
        
        public void OnScroll(PointerEventData eventData)
        {
            StopAutoScroll();
            if (autoScroll) StartAutoScrollIfNeeded();
        }

        private void ResetScrollTop()
        {
            if (contentRt)
                contentRt.anchoredPosition = new Vector2(contentRt.anchoredPosition.x, 0f);

            if (scrollRect)
                scrollRect.verticalNormalizedPosition = 1f;

            Canvas.ForceUpdateCanvases();
        }

        private void StartAutoScrollIfNeeded()
        {
            if (!scrollRect || !viewportRt || !contentRt) return;

            float overflow = contentRt.rect.height - viewportRt.rect.height;
            if (overflow <= 1f) return;

            _co = StartCoroutine(AutoScrollDown());
        }

        private IEnumerator AutoScrollDown()
        {
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            while (true)
            {
                while (_isDragging) 
                {
                    yield return null; 
                }

                float overflow = contentRt.rect.height - viewportRt.rect.height;
                if (overflow <= 1f) yield break;

                float targetY = overflow;

                while (contentRt.anchoredPosition.y < targetY - 0.01f)
                {
                    if (_isDragging) break;
                    
                    if (scrollRect) scrollRect.velocity = Vector2.zero;
                    
                    float y = contentRt.anchoredPosition.y;
                    y = Mathf.MoveTowards(y, targetY, scrollSpeed * Time.unscaledDeltaTime);
                    contentRt.anchoredPosition = new Vector2(contentRt.anchoredPosition.x, y);
                    yield return null;
                }

                if (_isDragging) continue;

                if (endDelay > 0f) yield return new WaitForSeconds(endDelay);
                if (!loop) yield break;

                contentRt.anchoredPosition = new Vector2(contentRt.anchoredPosition.x, 0f);
                if (startDelay > 0f) yield return new WaitForSeconds(startDelay);
            }
        }

        private void StopAutoScroll()
        {
            if (_co != null)
            {
                StopCoroutine(_co);
                _co = null;
            }
        }
        
        
    }
}