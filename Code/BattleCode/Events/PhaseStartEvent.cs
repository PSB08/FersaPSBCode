using PSB.Code.BattleCode.Enemies;
using PSW.Code.EventBus;

namespace PSB.Code.BattleCode.Events
{
    public struct PhaseStartEvent : IEvent
    {
        public int PhaseIndex;
        public int TotalPhases;
        public EnemySO[] PhaseEnemies;

        public PhaseStartEvent(int index, int total, EnemySO[] enemies)
        {
            PhaseIndex = index;
            TotalPhases = total;
            PhaseEnemies = enemies;
        }
    }
    
    public struct PhaseClearEvent : IEvent { }
    
    public struct SpawnAdditionalEnemiesEvent : IEvent
    {
        public EnemySO[] EnemiesToSpawn;

        public SpawnAdditionalEnemiesEvent(EnemySO[] enemies)
        {
            EnemiesToSpawn = enemies;
        }
    }
    
}