using System;
using UnityEngine;
using UnityEngine.UI;

namespace PSB.Code.BattleCode.UIs
{
    public class EnemyDetectUI : MonoBehaviour
    {
        [SerializeField] private Image detectionFillImage;
        
        private CanvasGroup _group;
        
        private void Awake()
        {
            _group = detectionFillImage.GetComponent<CanvasGroup>();
            Hide();
        }
        
        private void LateUpdate()
        {
            if (transform.parent != null)
            {
                Vector3 scale = detectionFillImage.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * Mathf.Sign(detectionFillImage.transform.parent.lossyScale.x);
                detectionFillImage.transform.localScale = scale;
            }
        }

        public void Show()
        {
            if (detectionFillImage != null)
            {
                _group.alpha = 1;
                detectionFillImage.fillAmount = 0f;
            }
        }

        public void Hide()
        {
            if (detectionFillImage != null)
            {
                _group.alpha = 0;
                detectionFillImage.fillAmount = 0f;
            }
        }
        
        public void UpdateProgress(float progress01)
        {
            if (detectionFillImage != null)
            {
                detectionFillImage.fillAmount = Mathf.Clamp01(progress01);
            }
        }
        
    }
}