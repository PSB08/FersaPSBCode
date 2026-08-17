using Code.Scripts.Enemies.BT;
using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using UnityEngine;
using YIS.Code.Effects;
using YIS.Code.Feedbacks;

namespace PSB.Code.BattleCode.Feedbacks
{
    public interface ITransformTargetFeedback
    {
        void SetTarget(Transform target);
    }

    public class StunEffectFeedback : Feedback, ITransformTargetFeedback
    {
        [Inject] private PoolManagerMono _poolManager;

        [SerializeField] private PoolItemSO effectPrefab;
        [SerializeField] private AnimParamSO effectParam;
        [SerializeField] private Transform target;
        [SerializeField] private bool parentToTarget = true;
        [SerializeField] private bool matchTargetRotation;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private Vector3 worldOffset;

        private PoolAnimatorEffect _effect;

        public void SetTarget(Transform targetTransform)
        {
            target = targetTransform;
        }

        public override void PlayFeedback()
        {
            StopFeedback();

            if (_poolManager == null)
            {
                Debug.LogError("[StunEffectFeedback] PoolManagerMono is missing.", this);
                return;
            }

            if (effectPrefab == null)
            {
                Debug.LogError("[StunEffectFeedback] Effect PoolItemSO is missing.", this);
                return;
            }

            _effect = _poolManager.Pop<PoolAnimatorEffect>(effectPrefab);
            if (_effect == null)
            {
                Debug.LogError("[StunEffectFeedback] Failed to pop stun effect from pool.", this);
                return;
            }

            Transform root = target != null ? target : transform;
            Transform effectTr = _effect.transform;

            if (parentToTarget)
            {
                effectTr.SetParent(root, false);
                effectTr.localPosition = localOffset;
                
                if (matchTargetRotation)
                    effectTr.localRotation = Quaternion.identity;
                else
                    effectTr.rotation = Quaternion.identity;
            }
            else
            {
                effectTr.SetParent(null, true);
                effectTr.SetPositionAndRotation(root.position + worldOffset, matchTargetRotation ? root.rotation : Quaternion.identity);
            }

            _effect.PlayClipEffect(effectTr.position, effectTr.rotation, effectParam.paramHash);
        }

        public override void StopFeedback()
        {
            if (_effect == null)
                return;

            PoolAnimatorEffect effect = _effect;
            _effect = null;

            effect.transform.SetParent(null, true);
            effect.DestroyObj();
        }

        private void OnDisable()
        {
            StopFeedback();
        }
        
    }
}
