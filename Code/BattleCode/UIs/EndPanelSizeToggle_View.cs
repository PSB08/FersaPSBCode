using DG.Tweening;
using UnityEngine;

namespace PSB.Code.BattleCode.UIs
{
    public class EndPanelSizeToggle_View : MonoBehaviour, IView<Vector2>
    {
        [SerializeField] private RectTransform panelRect;

        public void Init(Vector2 defaultSize)
        {
            if (panelRect == null) panelRect = transform as RectTransform;
            panelRect.sizeDelta = defaultSize;
        }

        public void SetData(Vector2 data) { }

        public void AnimateTo(Vector2 size, float time)
        {
            if (panelRect == null) panelRect = transform as RectTransform;

            panelRect.DOKill();
            panelRect.DOSizeDelta(size, time).SetUpdate(true);
        }
        
        public void AnimateToHeight(Vector2 size, float time)
        {
            if (panelRect == null) panelRect = transform as RectTransform;

            panelRect.DOKill();

            var cur = panelRect.sizeDelta;
            var next = new Vector2(cur.x, size.y);

            panelRect.DOSizeDelta(next, time).SetUpdate(true);
        }
        
    }
}