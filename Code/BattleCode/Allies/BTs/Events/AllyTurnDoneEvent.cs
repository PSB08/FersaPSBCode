using PSW.Code.EventBus;

namespace PSB.Code.BattleCode.Allies.BTs.Events
{
    public struct AllyTurnDoneEvent : IEvent
    {
        public readonly BattleAlly BattleAlly;
        public AllyTurnDoneEvent(BattleAlly battleAlly) => BattleAlly = battleAlly;
    }
}