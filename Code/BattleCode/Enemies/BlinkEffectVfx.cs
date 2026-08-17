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
        [SerializeField] private GameObject shieldRoot;
        [SerializeField] private TextMeshProUGUI shieldText;
        
        [SerializeField] private float duration = 0.5f; 
        
        private CanvasGroup _previewCanvasGroup;
        private Tween _hpValueTween;
        
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
        
        public void StartBlink(float currentHp, float maxHp, float currentShield, float finalDamage,
            float accumulatedDamage = 0)
        {
            CleanUp(); 
            
            if (hpBarFill == null || previewBarFill == null || maxHp <= 0) return;
            
            float baseFill = currentHp / maxHp;
            float totalDamage = Mathf.Max(0f, finalDamage + accumulatedDamage);
            float expectedShield = Mathf.Max(0f, currentShield - totalDamage);
            float healthDamage = Mathf.Max(0f, totalDamage - currentShield);
            float expectedHp = Mathf.Max(0f, currentHp - healthDamage);
            float afterDamageFill = expectedHp / maxHp;
            
            previewBarFill.gameObject.SetActive(true);
            previewBarFill.fillAmount = baseFill;    
            hpBarFill.fillAmount = afterDamageFill;
            
            if (hpText != null || shieldText != null)
            {
                SetHpText(currentHp, currentShield);
                
                _hpValueTween = DOVirtual.Float(0f, totalDamage, duration, previewDamage =>
                {
                    float previewShield = Mathf.Max(0f, currentShield - previewDamage);
                    float previewHealthDamage = Mathf.Max(0f, previewDamage - currentShield);
                    float previewHp = Mathf.Max(0f, currentHp - previewHealthDamage);
                    
                    SetHpText(previewHp, previewShield);
                }).SetUpdate(true).OnComplete(() => SetHpText(expectedHp, expectedShield));
                
                if (hpText != null)
                    hpText.DOFade(0.3f, duration).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
                
                if (shieldText != null && shieldText.gameObject.activeInHierarchy)
                    shieldText.DOFade(0.3f, duration).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
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
        
        public void StopBlink(float currentHp, float maxHp, float currentShield)
        {
            CleanUp();
            
            if (hpBarFill != null && maxHp > 0)
            {
                hpBarFill.fillAmount = currentHp / maxHp;
            }
            if (hpText != null || shieldText != null)
            {
                SetHpText(currentHp, currentShield);
            }
        }
        
        private void SetHpText(float hp, float shield)
        {
            int roundedHp = Mathf.Max(0, Mathf.RoundToInt(hp));
            int roundedShield = Mathf.Max(0, Mathf.RoundToInt(shield));
            bool hasShield = roundedShield > 0;
            
            if (hpText != null)
                hpText.SetText(roundedHp.ToString());
            
            if (shieldRoot != null)
                shieldRoot.SetActive(hasShield);
            else if (shieldText != null)
                shieldText.gameObject.SetActive(hasShield);
            
            if (shieldText != null)
                shieldText.SetText(roundedShield.ToString());
        }
        
        private void CleanUp()
        {
            if (_hpValueTween != null && _hpValueTween.IsActive())
                _hpValueTween.Kill();
            
            _hpValueTween = null;
            
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
            
            if (shieldText != null)
            {
                shieldText.DOKill();
                Color c = shieldText.color;
                c.a = 1f;
                shieldText.color = c;
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
