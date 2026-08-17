using PSB.Code.BattleCode.Enemies;
using PSW.Code.EventBus;

namespace PSB.Code.BattleCode.Events
{
    public struct BattleEncounterStartEvent : IEvent
    {
        public EnemySO[] Enemies;

        public BattleEncounterStartEvent(EnemySO[] enemies)
        {
            Enemies = enemies;
        }
    }
    
}
