namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemyTurnStartedSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
        public bool IsPlayerTurn { get; }
        
        public EnemyTurnStartedSignal(BattleEnemy enemy, bool isPlayerTurn)
        {
            Enemy = enemy;
            IsPlayerTurn = isPlayerTurn;
        }
        
    }
}
