using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using YIS.Code.Feedbacks;
using Random = UnityEngine.Random;

namespace PSB.Code.BattleCode.Feedbacks
{
    public class HitShakeFeedback : Feedback
    {
        [SerializeField] private float duration = 0.3f;
        
        [SerializeField] private float intensityX = 0.5f; 
        [SerializeField] private float intensityY = 0.3f; 
        
        [SerializeField] private Transform target;

        public override void PlayFeedback()
        {
            if (target == null) return;

            target.DOComplete();
            
            Vector3 punchDirection = new Vector3(-intensityX, intensityY, 0f);

            target.DOPunchPosition(punchDirection, duration, vibrato: 5, elasticity: 0.5f);
        }

        public override void StopFeedback()
        {
            if (target != null)
            {
                target.DOComplete(); 
            }
        }
        
    }
}