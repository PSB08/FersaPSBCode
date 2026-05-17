using CIW.Code;
using PSB_Lib.ObjectPool.RunTime;
using PSB.Code.BattleCode.Skills;
using PSW.Code.EventBus;
using UnityEngine;
using Work.PSB.Code.CoreSystem.Sounds;
using YIS.Code.Effects;
using YIS.Code.Events;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Entities
{
    public class EntitySkillEffectExecutor : IExecuteEffect
    {
        private readonly BaseSkillCache _cache;
        private readonly PoolManagerMono _poolManager;
 
        public EntitySkillEffectExecutor(BaseSkillCache cache, PoolManagerMono poolManager)
        {
            _cache = cache;
            _poolManager = poolManager;
        }
        
        public void PlayEffectForTarget(BaseSkill skill, Transform targetTrans, Entity target, SkillDataSO vfxSourceData)
        {
            if (_poolManager == null || target == null)
                return;
            
            SkillDataSO vfxData = vfxSourceData != null ? vfxSourceData : skill.SkillData;
            if (vfxData == null) return;

            if (vfxData.animatorEffect != null)
            {
                var p = _poolManager.Pop<PoolAnimatorEffect>(vfxData.animatorEffect);
                var evt = p.GetComponentInChildren<EffectTrigger>();
                if (evt != null) evt.OnEndTrigger += () => p.DestroyObj();
                Vector3 effectPos = vfxData.playEffectOnSelf
                    ? _cache.transform.position
                    : targetTrans.position;
                Quaternion rot = vfxData.playEffectOnFlip
                    ? Quaternion.Euler(0f, 180f, 0f)
                    : Quaternion.identity;

                int hash = vfxData.effectParam != null ? vfxData.effectParam.paramHash : 0;
                p.PlayClipEffect(effectPos, rot, hash);
            }

            if (vfxData.attackSound != null)
            {
                PlaySFXEvent soundEvt = SoundEvents.PlaySFXEvent.
                    Initialize(_cache.transform.position, vfxData.attackSound);
                Bus<PlaySFXEvent>.Raise(soundEvt);
            }
            else
            {
                Debug.LogWarning("사운드 데이터가 없습니다.");
            }
        }
        
    }
}