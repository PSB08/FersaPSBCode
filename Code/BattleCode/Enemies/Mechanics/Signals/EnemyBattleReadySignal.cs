namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemyBattleReadySignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
        
        public EnemyBattleReadySignal(BattleEnemy enemy)
        {
            Enemy = enemy;
        }
        
    }
}
