using PSB.Code.BattleCode.Entities;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Enemies
{
    public class EnemyRangePreviewVfx : MonoBehaviour, IModule
    {
        [SerializeField] private GameObject previewFx;
        [SerializeField] private GameObject previewObj;
        
        private BlinkEffectVfx _blinkEffect;
        private EntityHealth _health;

        public void Initialize(ModuleOwner owner)
        {
            _blinkEffect = GetComponent<BlinkEffectVfx>();
            _health = owner.GetModule<EntityHealth>();
        }

        public void SetPreview(bool on, float expectedDamage, float accumulatedDamage = 0) 
        {
            if (on)
            {
                if (previewFx != null && !previewFx.activeSelf) 
                    previewFx.SetActive(true);
                
                previewFx?.GetComponent<Animator>()?.Play("SelectAnimPlay", 0, 0f);
                previewObj?.GetComponent<Animator>()?.Play("CheckerPlay", 0, 0f);
        
                if (_blinkEffect != null && _health != null)
                {
                    _blinkEffect.StartBlink(_health.CurrentHealth, 
                        _health.MaxHealth, expectedDamage, accumulatedDamage); 
                }
            }
            else
            {
                if (previewFx != null) 
                    previewFx.SetActive(false);
        
                if (_blinkEffect != null && _health != null)
                    _blinkEffect.StopBlink(_health.CurrentHealth, _health.MaxHealth);
            }
        }
        
        public void SetPreview(bool on) 
        {
            if (on)
            {
                if (previewFx != null && !previewFx.activeSelf) 
                    previewFx.SetActive(true);
                
                previewFx?.GetComponent<Animator>()?.Play("SelectAnimPlay", 0, 0f);
                previewObj?.GetComponent<Animator>()?.Play("CheckerPlay", 0, 0f);
            }
            else
            {
                if (previewFx != null) 
                    previewFx.SetActive(false);
        
                if (_blinkEffect != null && _health != null)
                    _blinkEffect.StopBlink(_health.CurrentHealth, _health.MaxHealth);
            }
        }
        
    }
}