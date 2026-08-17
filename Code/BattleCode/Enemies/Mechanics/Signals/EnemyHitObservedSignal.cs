namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemyHitObservedSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
        
        public EnemyHitObservedSignal(BattleEnemy enemy)
        {
            Enemy = enemy;
        }
        
    }
}
