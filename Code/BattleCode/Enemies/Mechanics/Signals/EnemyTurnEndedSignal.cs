namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemyTurnEndedSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
        public bool IsPlayerTurn { get; }
        
        public EnemyTurnEndedSignal(BattleEnemy enemy, bool isPlayerTurn)
        {
            Enemy = enemy;
            IsPlayerTurn = isPlayerTurn;
        }
        
    }
}
