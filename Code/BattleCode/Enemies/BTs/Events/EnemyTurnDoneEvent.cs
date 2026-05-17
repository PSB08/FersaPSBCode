using PSW.Code.EventBus;

namespace PSB.Code.BattleCode.Enemies.BTs.Events
{
    public struct EnemyTurnDoneEvent : IEvent
    {
        public readonly BattleEnemy BattleEnemy;
        public EnemyTurnDoneEvent(BattleEnemy battleEnemy) => BattleEnemy = battleEnemy;
    }
}