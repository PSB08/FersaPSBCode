using PSW.Code.EventBus;
using YIS.Code.Items;

namespace PSB.Code.CoreSystem.Events
{
    public struct BattleLootConsumedEvent : IEvent
    {
        public ItemDataSO Item;
        public int Amount;

        public BattleLootConsumedEvent(ItemDataSO item, int amount)
        {
            Item = item;
            Amount = amount;
        }
    }

    public struct ItemGainedEvent : IEvent
    {
        public ItemDataSO Item;
        public int Amount;
    }

    public struct RequestSaveEvent : IEvent
    {
    }
    
    public struct BattleLootAddedEvent : IEvent
    {
        public ItemDataSO Item;
        public int Amount;

        public BattleLootAddedEvent(ItemDataSO item, int amount)
        {
            Item = item;
            Amount = amount;
        }
    }
    
    public struct RollbackBattleLootEvent : IEvent
    {
    }

}