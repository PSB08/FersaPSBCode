using PSB.Code.BattleCode.Enemies.Mechanics.Signals;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules
{
    public readonly struct EnemyMechanicExecutionContext
    {
        public EnemyMechanicContext Mechanics { get; }
        public EnemyMechanicSO Source { get; }
        public object Signal { get; }
        
        public BattleEnemy SignalEnemy
        {
            get
            {
                return Signal is IEnemyMechanicSignal enemySignal && enemySignal.Enemy != null
                    ? enemySignal.Enemy
                    : Mechanics.Enemy;
            }
        }
        
        public EnemyMechanicExecutionContext(EnemyMechanicContext mechanics,
            EnemyMechanicSO source, object signal)
        {
            Mechanics = mechanics;
            Source = source;
            Signal = signal;
        }
        
    }
}
