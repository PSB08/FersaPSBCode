namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemyDeathStartedSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
		
        public EnemyDeathStartedSignal(BattleEnemy enemy)
        {
            Enemy = enemy;
        }
		
    }
	
}
