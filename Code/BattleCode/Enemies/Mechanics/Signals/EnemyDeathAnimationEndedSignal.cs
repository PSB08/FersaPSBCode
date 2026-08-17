namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemyDeathAnimationEndedSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
		
        public EnemyDeathAnimationEndedSignal(BattleEnemy enemy)
        {
            Enemy = enemy;
        }
		
    }
	
}
