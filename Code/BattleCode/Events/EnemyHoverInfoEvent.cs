using PSB.Code.BattleCode.Enemies;
using PSW.Code.EventBus;

namespace PSB.Code.BattleCode.Events
{
    public struct EnemyHoverInfoEvent : IEvent
    {
        public readonly BattleEnemy Enemy;
        public readonly bool Show;

        public EnemyHoverInfoEvent(BattleEnemy enemy, bool show)
        {
            Enemy = enemy;
            Show = show;
        }
    }
    
    public struct EnemyInfoCloseEvent : IEvent { }
    
}