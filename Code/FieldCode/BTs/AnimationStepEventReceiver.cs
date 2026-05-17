using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSW.Code.EventBus;
using UnityEngine;
using Work.PSB.Code.CoreSystem.Sounds;
using YIS.Code.Effects;

namespace Work.PSB.Code.FieldCode.BTs
{
    public class AnimationStepEventReceiver : MonoBehaviour
    {
        [Header("Step SFX")]
        [SerializeField] private SoundSO stepSound; 
        
        [Header("Step VFX")]
        [SerializeField] private PoolItemSO stepVfxItem;
        [SerializeField] private Transform vfxSpawnPoint;

        [Inject] private PoolManagerMono _poolManager;

        public void OnStep()
        {
            if (stepSound != null)
            {
                PlaySFXEvent soundEvt = SoundEvents.PlaySFXEvent.Initialize(transform.position, stepSound);
                Bus<PlaySFXEvent>.Raise(soundEvt);
            }

            if (stepVfxItem != null && _poolManager != null)
            {
                var p = _poolManager.Pop<PoolAnimatorEffect>(stepVfxItem);
                var evt = p.GetComponentInChildren<EffectTrigger>();
                if (evt != null) evt.OnEndTrigger += () => p.DestroyObj();
                
                Vector3 spawnPos = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
                
                bool isFacingLeft = transform.lossyScale.x < 0f;

                Quaternion spawnRot = isFacingLeft
                    ? Quaternion.identity
                    : Quaternion.Euler(0f, 180f, 0f);
                
                p.PlayClipEffect(spawnPos, spawnRot, Animator.StringToHash("FOOT"));
            }
        }
        
    }
}