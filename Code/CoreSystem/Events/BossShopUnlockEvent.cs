using PSB.Code.BattleCode.UIs.BossShopUI;
using PSW.Code.EventBus;
using YIS.Code.Items;

namespace PSB.Code.CoreSystem.Events
{
    public struct BossShopUnlockEvent : IEvent
    {
        public UnlockDataSO Item;

        public BossShopUnlockEvent(UnlockDataSO item)
        {
            Item = item;
        }
        
    }
    
    public struct BossShopRefreshEvent : IEvent
    {
    }

    public struct ResetAllPrefEvent : IEvent
    {
        
    }
    

    public struct BossShopUIActiveEvent : IEvent
    {
        public bool IsActive;

        public BossShopUIActiveEvent(bool isActive)
        {
            this.IsActive = isActive;
        }
    }
    
    
}