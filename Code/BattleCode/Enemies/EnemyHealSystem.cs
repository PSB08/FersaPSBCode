using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Effects;

namespace PSB.Code.BattleCode.Enemies
{
    public class EnemyHealSystem : MonoBehaviour
    {
        [SerializeField] private PoolItemSO healEffectPrefab;
     
        [Inject] private PoolManagerMono _poolManager;

        private void OnEnable()
        {
            Bus<EnemyHealRequest>.OnEvent += OnEnemyHeal;
        }

        private void OnDisable()
        {
            Bus<EnemyHealRequest>.OnEvent -= OnEnemyHeal;
        }

        private void OnEnemyHeal(EnemyHealRequest req)
        {
            if (req.target == null) return;
            float beforeHealth = req.target.CurrentHealth;

            req.target.Heal(req.value, req.mode);
            
            float actualHealed = req.target.CurrentHealth - beforeHealth;
            if (actualHealed <= 0f) return;
            
            Bus<HealTextUiEvent>.Raise(new HealTextUiEvent(req.target.transform.position, actualHealed));

            Vector3 targetPos = req.target.transform.position;
            Quaternion rot = req.target.transform.rotation;
            
            PoolAnimatorEffect p = _poolManager.Pop<PoolAnimatorEffect>(healEffectPrefab);
            
            EffectTrigger evt = p.GetComponentInChildren<EffectTrigger>();
            evt.OnEndTrigger += () => p.DestroyObj();
            
            p.transform.SetParent(req.target.transform);
            p.transform.localScale = Vector3.one * 0.7f;
            p.PlayClipEffect(targetPos, rot, Animator.StringToHash("SLASH"));
        }
        
    }
}