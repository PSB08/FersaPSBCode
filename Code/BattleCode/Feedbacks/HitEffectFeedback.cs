using Code.Scripts.Enemies.BT;
using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using Unity.Mathematics;
using UnityEngine;
using YIS.Code.Effects;
using YIS.Code.Feedbacks;

namespace PSB.Code.BattleCode.Feedbacks
{
    public class HitEffectFeedback : Feedback
    {
        [Inject] private PoolManagerMono _poolManagerMono;
        
        [SerializeField] private PoolItemSO animatorEffect;
        [SerializeField] private AnimParamSO effectParam;

        private void Start()
        {
            if (_poolManagerMono == null)
                _poolManagerMono = FindAnyObjectByType<PoolManagerMono>();
        }

        public override void PlayFeedback()
        {
            PoolAnimatorEffect p = _poolManagerMono.Pop<PoolAnimatorEffect>(animatorEffect);
            EffectTrigger evt = p.GetComponentInChildren<EffectTrigger>();
            evt.OnEndTrigger += () => p.DestroyObj();
            p.PlayClipEffect(transform.position, quaternion.identity, effectParam.paramHash);
        }

        public override void StopFeedback()
        {
            
        }
        
    }
}