using PSB.Code.BattleCode.Enemies.Mechanics.Phases;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemyPhaseEnteredSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
        public EnemyPhaseDefinition Phase { get; }
        public int PhaseNumber { get; }
        public int TotalPhaseCount { get; }
        
        public EnemyPhaseEnteredSignal(BattleEnemy enemy, EnemyPhaseDefinition phase,
            int phaseNumber, int totalPhaseCount)
        {
            Enemy = enemy;
            Phase = phase;
            PhaseNumber = phaseNumber;
            TotalPhaseCount = totalPhaseCount;
        }
        
    }
}
