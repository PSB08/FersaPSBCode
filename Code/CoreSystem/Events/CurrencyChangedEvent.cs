using PSW.Code.EventBus;
using YIS.Code.Defines;

namespace PSB.Code.CoreSystem.Events
{
    public struct CurrencyChangedEvent : IEvent
    {
        public ItemType Type;
        public int OldValue;
        public int NewValue;

        public CurrencyChangedEvent(ItemType type, int oldValue, int newValue)
        {
            Type = type;
            OldValue = oldValue;
            NewValue = newValue;
        }
        
    }

    public struct BattleCurrencyAddedEvent : IEvent
    {
        public ItemType Type;
        public int Amount;

        public BattleCurrencyAddedEvent(ItemType type, int amount)
        {
            Type = type;
            Amount = amount;
        }
    }
    
}