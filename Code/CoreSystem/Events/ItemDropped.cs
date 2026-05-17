using PSB.Code.BattleCode.Items;
using PSW.Code.EventBus;

namespace PSB.Code.CoreSystem.Events
{
    public enum LootApplyMode
    {
        Immediate,
        BattleSession
    }
    
    public class ItemDropped : IEvent
    {
        public DropResultTable Result { get; private set; }
        public LootApplyMode ApplyMode { get; private set; }

        public ItemDropped Init(DropResultTable result, LootApplyMode applyMode = LootApplyMode.BattleSession)
        {
            Result = result;
            ApplyMode = applyMode;
            return this;
        }
        
    }
}