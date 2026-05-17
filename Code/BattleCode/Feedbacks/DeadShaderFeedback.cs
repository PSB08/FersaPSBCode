using System;
using DG.Tweening;
using UnityEngine;
using YIS.Code.Feedbacks;

namespace PSB.Code.BattleCode.Feedbacks
{
    public class DeadShaderFeedback : Feedback
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private string fadePropertyName = "_Fade"; 
        [SerializeField] private float duration = 1.5f;

        private Material _mat;
        
        public void PlayFeedback(Action onComplete)
        {
            if (targetRenderer == null)
            {
                Debug.LogError("[Feedback] Target Renderer 없는데요");
                onComplete?.Invoke();
                return;
            }
    
            _mat = targetRenderer.material;
    
            if (_mat == null)
            {
                Debug.LogError("[Feedback] Material이 Null");
                onComplete?.Invoke();
                return;
            }
    
            _mat.DOKill();
    
            _mat.SetFloat(fadePropertyName, 1f);
            _mat.DOFloat(0f, fadePropertyName, duration)
                .OnComplete(() => 
                {
                    onComplete?.Invoke();
                });
        }

        public override void PlayFeedback()
        {
            PlayFeedback(null);
        }

        public override void StopFeedback()
        {
            if (_mat != null)
            {
                _mat.DOKill();
            }
        }
        
    }
}