using System.Collections;
using CIW.Code;
using CIW.Code.Entities;
using CIW.Code.System.Events;
using DG.Tweening;
using PSB_Lib.Dependencies;
using PSW.Code.EventBus;
using UnityEngine;
using Work.CSH.Scripts.Managers;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Players
{
    public class BattlePlayer : Entity, IModule
    {
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }
        [field: SerializeField] public TurnManagerSO TurnManager { get; private set; }

        private EntityAnimator _animator;

        public void Initialize(ModuleOwner owner)
        {
            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);
            _animator = GetModule<EntityAnimator>();
        }

        protected override void Start()
        {
            base.Start();
            int idleHash = Animator.StringToHash("IDLE");

            _animator.PlayClip(idleHash);
        }

        protected override void Die()
        {
        }

        public void IdleAnimRoute()
        {
            StartCoroutine(CoIdleAnimRoute());
        }

        public void AttackAnimRoute()
        {
            StartCoroutine(CoAttackAnimRoute());
        }

        public void DeadAnimRoute()
        {
            StartCoroutine(CoDeadAnimRoute());
        }
        
        private IEnumerator CoIdleAnimRoute()
        {
            int idleHash = Animator.StringToHash("IDLE");

            _animator.PlayClip(idleHash);
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator CoDeadAnimRoute()
        {
            Bus<BattleEnd>.Raise(new BattleEnd(false));
            int deadHash = Animator.StringToHash("DEAD");

            _animator.PlayClip(deadHash);
            yield return new WaitForSeconds(0.7f);
            DestroyEntity();
        }

        private IEnumerator CoAttackAnimRoute()
        {
            int idleHash = Animator.StringToHash("IDLE");
            int attackHash = Animator.StringToHash("ATTACK");

            _animator.PlayClip(attackHash);
            yield return new WaitForSeconds(0.8f);
            _animator.PlayClip(idleHash);
        }

        private readonly object _dashTweenId = "PLAYER_DASH";
        
        public Tween AnticipateAndDashTo(Vector3 dashPos, float backDistance = 0.1f, 
            float backTime = 0.07f, float dashTime = 0.04f)
        {
            Vector3 start = transform.position;

            Vector3 dir = dashPos - start;
            dir.Normalize();
            
            Vector3 backPos = start - dir * backDistance;
            backPos.z = start.z;

            Sequence seq = DOTween.Sequence()
                .SetId(_dashTweenId)
                .Append(transform.DOMove(backPos, backTime).SetEase(Ease.OutQuad))
                .Append(transform.DOMove(dashPos, dashTime).SetEase(Ease.InQuad))  
                .SetUpdate(UpdateType.Normal, true);

            return seq;
        }

        public Tween ReturnTo(Vector3 startPos)
        {
            return transform
                .DOMove(startPos, 0.08f)
                .SetEase(Ease.InQuad)
                .SetId(_dashTweenId);
        }
        
        
    }
}