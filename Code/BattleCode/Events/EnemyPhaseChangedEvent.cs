using PSB.Code.BattleCode.Enemies;
using PSW.Code.EventBus;

namespace PSB.Code.BattleCode.Events
{
    public struct EnemyPhaseChangedEvent : IEvent
    {
        public BattleEnemy TargetEnemy;
        public int CurrentPhaseNum;
        public int TotalPhases;

        public EnemyPhaseChangedEvent(BattleEnemy targetEnemy, int currentPhaseNum, int totalPhases)
        {
            TargetEnemy = targetEnemy;
            CurrentPhaseNum = currentPhaseNum;
            TotalPhases = totalPhases;
        }
        
    }
}