using System;
using CIW.Code.Entities;
using UnityEngine;
using YIS.Code.Modules;

namespace Work.PSB.Code.FieldCode
{
    public class FieldBoxCollectible : ModuleOwner, IModule
    {
        [SerializeField] private string boxId;
        public string BoxId => boxId;

        public EntityAnimator Animator { get; private set; }
        private EntityAnimatorTrigger _trigger;

        public void Initialize(ModuleOwner owner)
        {
            Animator = owner.GetModule<EntityAnimator>();
            _trigger = owner.GetModule<EntityAnimatorTrigger>();
        }

        private void OnEnable()
        {
            _trigger.OnAnimationEndTrigger += EndAnim;
        }

        private void OnDisable()
        {
            _trigger.OnAnimationEndTrigger -= EndAnim;
        }

        private void EndAnim()
        {
            gameObject.SetActive(false);
        }
        
    }
}