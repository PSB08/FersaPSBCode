using Code.Scripts.Enemies.BT;
using Code.Scripts.Entities;
using UnityEngine;
using YIS.Code.Modules;

namespace Work.PSB.Code.FieldCode.MapSaves
{
    public class ShopPortalEntity : ModuleOwner, IModule
    {
        public AnimParamSO idleParam;
        public EntityRenderer Renderer { get; private set; }

        public void Initialize(ModuleOwner owner)
        {
            Renderer = owner.GetModule<EntityRenderer>();
        }

        protected override void Awake()
        {
            base.Awake();
            Renderer.ChangeClip(idleParam);
        }
        
    }
}