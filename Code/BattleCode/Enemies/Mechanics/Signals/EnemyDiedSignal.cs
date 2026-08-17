namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemyDiedSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
        
        public EnemyDiedSignal(BattleEnemy enemy)
        {
            Enemy = enemy;
        }
        
    }
}
