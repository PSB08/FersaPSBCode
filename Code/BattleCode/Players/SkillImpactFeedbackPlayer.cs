using CIW.Code.System.Events;
using Code.Scripts.Enemies.BT;
using PSB_Lib.ObjectPool.RunTime;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Effects;
using YIS.Code.Events;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Players
{
    public class SkillImpactFeedbackPlayer : MonoBehaviour
    {
        [Header("Impact Effect")]
        [SerializeField] private PoolItemSO strongImpactEffect;
        [SerializeField] private AnimParamSO strongImpactEffectParam;

        [Header("Damage Condition")]
        [SerializeField] private float damageThreshold = 100f;

        [Header("Impulse")]
        [SerializeField] private float heavyAttackImpulsePower = 1f;
        [SerializeField] private float allChainingImpulsePower = 1.2f;
        [SerializeField] private float highDamageImpulsePower = 1.2f;

        [Header("Position")]
        [SerializeField] private bool playEffectOnTarget = true;

        private PoolManagerMono _poolManager;
        private bool _isAllChainingAttack;

        public bool IsAllChainingAttack => _isAllChainingAttack;

        public void Initialize(PoolManagerMono poolManager)
        {
            _poolManager = poolManager;
        }

        private void OnEnable()
        {
            Bus<AllChainingEvent>.OnEvent += OnAllChaining;
        }

        private void OnDisable()
        {
            Bus<AllChainingEvent>.OnEvent -= OnAllChaining;
            _isAllChainingAttack = false;
        }

        private void OnAllChaining(AllChainingEvent evt)
        {
            _isAllChainingAttack = true;
        }

        public void PlayIfNeeded(SkillDataSO skillData, Transform targetTr, Transform userTr)
        {
            if (skillData == null)
                return;

            float damage = Mathf.Max(0f, skillData.damage);

            bool isHeavyAttack = skillData.isStrongAttack;
            bool isAllChainingAttack = _isAllChainingAttack;
            bool isHighDamage = damage >= damageThreshold;

            if (!isHeavyAttack && !isAllChainingAttack && !isHighDamage)
                return;

            Vector3 effectPos = GetEffectPosition(targetTr, userTr);

            PlayEffect(effectPos);
            PlayImpulse(isHeavyAttack, isAllChainingAttack, isHighDamage);
        }

        public void ResetAllChainingAttack()
        {
            _isAllChainingAttack = false;
        }

        private Vector3 GetEffectPosition(Transform targetTr, Transform userTr)
        {
            if (playEffectOnTarget && targetTr != null)
                return targetTr.position;

            if (userTr != null)
                return userTr.position;

            return transform.position;
        }

        private void PlayEffect(Vector3 position)
        {
            if (_poolManager == null || strongImpactEffect == null)
                return;

            PoolAnimatorEffect p = _poolManager.Pop<PoolAnimatorEffect>(strongImpactEffect);
            if (p == null)
                return;

            EffectTrigger evt = p.GetComponentInChildren<EffectTrigger>();
            if (evt != null)
                evt.OnEndTrigger += () => p.DestroyObj();

            int hash = strongImpactEffectParam != null ? strongImpactEffectParam.paramHash : 0;
            p.PlayClipEffect(position, Quaternion.identity, hash);
        }

        private void PlayImpulse(bool isHeavyAttack, bool isAllChainingAttack, bool isHighDamage)
        {
            float impulsePower = 0f;

            if (isHeavyAttack)
                impulsePower = Mathf.Max(impulsePower, heavyAttackImpulsePower);

            if (isAllChainingAttack)
                impulsePower = Mathf.Max(impulsePower, allChainingImpulsePower);

            if (isHighDamage)
                impulsePower = Mathf.Max(impulsePower, highDamageImpulsePower);

            if (impulsePower <= 0f)
                return;

            Bus<ImpulseEvent>.Raise(new ImpulseEvent(Vector3.one * impulsePower));
        }
        
    }
}
