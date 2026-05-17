using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace PSB.Code.BattleCode.Enemies
{
    public class BlinkEffectVfx : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private SpriteRenderer fireEffect;
        
        [Header("Fill Controllers")]
        [SerializeField] private Image hpBarFill;
        [SerializeField] private Image previewBarFill;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI hpText;

        [SerializeField] private float duration = 0.5f; 
        
        private CanvasGroup _previewCanvasGroup;

        private void Awake()
        {
            if (previewBarFill != null) 
            {
                previewBarFill.gameObject.SetActive(false);
                
                _previewCanvasGroup = previewBarFill.GetComponent<CanvasGroup>();
                if (_previewCanvasGroup == null)
                    _previewCanvasGroup = previewBarFill.gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void StartBlink(float currentHp, float maxHp, float finalDamage, float accumulatedDamage = 0)
        {
            CleanUp(); 

            if (hpBarFill == null || previewBarFill == null || maxHp <= 0) return;

            float baseFill = currentHp / maxHp;
            
            float totalDamage = finalDamage + accumulatedDamage;
            float totalDamagePercent = totalDamage / maxHp;
            float afterDamageFill = Mathf.Max(0, baseFill - totalDamagePercent);

            previewBarFill.gameObject.SetActive(true);
            previewBarFill.fillAmount = baseFill;    
            hpBarFill.fillAmount = afterDamageFill;

            if (hpText != null)
            {
                float expectedHp = Mathf.Max(0, currentHp - totalDamage);
                hpText.text = Mathf.CeilToInt(expectedHp).ToString();
                
                hpText.DOFade(0.3f, duration).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }

            if (_previewCanvasGroup != null)
            {
                _previewCanvasGroup.DOKill();
                _previewCanvasGroup.alpha = 1f;
                _previewCanvasGroup.DOFade(0.2f, duration).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }

            if (targetRenderer != null)
            {
                targetRenderer.DOKill();
                Color c = targetRenderer.color; c.a = 1f; targetRenderer.color = c;
                targetRenderer.DOFade(0.5f, duration).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }

            if (fireEffect != null)
            {
                fireEffect.DOKill();
                Color c = fireEffect.color; c.a = 1f; fireEffect.color = c;
                fireEffect.DOFade(0.5f, duration).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }
        }

        public void StopBlink(float currentHp, float maxHp)
        {
            CleanUp();
            
            if (hpBarFill != null && maxHp > 0)
            {
                hpBarFill.fillAmount = currentHp / maxHp;
            }
            if (hpText != null)
            {
                hpText.text = Mathf.CeilToInt(currentHp).ToString();
            }
        }

        private void CleanUp()
        {
            if (previewBarFill != null)
                previewBarFill.gameObject.SetActive(false);

            if (_previewCanvasGroup != null)
            {
                _previewCanvasGroup.DOKill();
                _previewCanvasGroup.alpha = 1f;
            }

            if (hpText != null) 
            { 
                hpText.DOKill();
                Color c = hpText.color; c.a = 1f; hpText.color = c; 
            }

            if (targetRenderer != null) 
            {
                targetRenderer.DOKill();
                Color c = targetRenderer.color; c.a = 1f; targetRenderer.color = c;
            }
            
            if (fireEffect != null) 
            {
                fireEffect.DOKill();
                Color c = fireEffect.color; c.a = 1f; fireEffect.color = c;
            }
        }
        
    }
}