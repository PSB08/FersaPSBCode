using Code.Scripts.Enemies.BT;
using Code.Scripts.Entities;
using YIS.Code.Modules;

namespace Work.PSB.Code.FieldCode.MapSaves
{
    public class DummyPortalEntity : ModuleOwner, IModule
    {
        public AnimParamSO defaultParam;
        
        public EntityRenderer Renderer { get; private set; }
        
        public void Initialize(ModuleOwner owner)
        {
            Renderer = owner.GetModule<EntityRenderer>();
            Renderer.ChangeClip(defaultParam);
        }
        
    }
}