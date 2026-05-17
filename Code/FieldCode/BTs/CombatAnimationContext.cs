using Code.Scripts.Entities;
using UnityEngine;
using YIS.Code.Modules;

namespace Work.PSB.Code.FieldCode.BTs
{
    public class CombatAnimationContext : MonoBehaviour, IModule
    {
        [SerializeField] private EntityRenderer entityRenderer;

        public bool HitTriggered { get; private set; }
        public bool EndTriggered { get; private set; }
        public bool DeadEndTriggered { get; private set; }
        
        public void Initialize(ModuleOwner owner)
        {
        }

        private void OnEnable()
        {
            entityRenderer.OnAnimationHitTrigger += HandleHit;
            entityRenderer.OnAnimationEndTrigger += HandleEnd;
            entityRenderer.OnDeadEndTrigger += HandleDead;
        }

        private void OnDisable()
        {
            entityRenderer.OnAnimationHitTrigger -= HandleHit;
            entityRenderer.OnAnimationEndTrigger -= HandleEnd;
            entityRenderer.OnDeadEndTrigger += HandleDead;
        }

        private void HandleDead()
        {
            DeadEndTriggered = true;
        }

        public void ResetFlags()
        {
            HitTriggered = false;
            EndTriggered = false;
            DeadEndTriggered = false;
        }

        private void HandleHit()
        {
            HitTriggered = true;
        }

        private void HandleEnd()
        {
            EndTriggered = true;
        }
        
    }
}