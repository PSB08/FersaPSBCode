using PSW.Code.EventBus;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Events
{
    public struct BuffListOpenEvent : IEvent
    {
        public ModuleOwner ModuleOwner;
        public BuffListOpenEvent(ModuleOwner owner) => ModuleOwner = owner;
        
    }
    
    public readonly struct BuffListCloseEvent : IEvent
    { }
    
}