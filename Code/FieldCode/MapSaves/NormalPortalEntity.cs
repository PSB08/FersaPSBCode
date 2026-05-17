using Code.Scripts.Enemies.BT;
using Code.Scripts.Entities;
using UnityEngine;
using YIS.Code.Modules;

namespace Work.PSB.Code.FieldCode.MapSaves
{
    public class NormalPortalEntity : ModuleOwner, IModule
    {
        [SerializeField] private GameObject dummyPortal;

        public AnimParamSO idleParam;
        public AnimParamSO openParam;

        private bool _isOpened;
        
        public EntityRenderer Renderer { get; private set; }
        
        public void Initialize(ModuleOwner owner)
        {
            Renderer = owner.GetModule<EntityRenderer>();
        }

        protected override void Awake()
        {
            base.Awake();
            SetClosedVisual();
        }

        private void OnEnable()
        {
            if (Renderer != null)
            {
                Renderer.OnAnimationEndTrigger -= HandleAnimationEnd;
                Renderer.OnAnimationEndTrigger += HandleAnimationEnd;
            }
        }

        private void OnDisable()
        {
            if (Renderer != null)
                Renderer.OnAnimationEndTrigger -= HandleAnimationEnd;
        }

        public void Open()
        {
            if (_isOpened) return;
            _isOpened = true;

            gameObject.SetActive(true);

            if (dummyPortal != null)
                dummyPortal.SetActive(false);

            Renderer.ChangeClip(openParam);
        }

        public void SetClosedVisual()
        {
            _isOpened = false;

            if (dummyPortal != null)
                dummyPortal.SetActive(true);

            gameObject.SetActive(false);
        }

        private void HandleAnimationEnd()
        {
            if (!_isOpened) return;
            Renderer.ChangeClip(idleParam);
        }
        
    }
}