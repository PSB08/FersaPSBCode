namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemyBattleStartedSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
        
        public EnemyBattleStartedSignal(BattleEnemy enemy)
        {
            Enemy = enemy;
        }
        
    }
}
