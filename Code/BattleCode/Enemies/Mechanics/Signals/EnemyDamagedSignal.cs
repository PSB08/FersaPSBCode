namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemyDamagedSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
        public float PreviousHealth { get; }
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
        public float Damage { get; }
        
        public EnemyDamagedSignal(BattleEnemy enemy, float previousHealth,
            float currentHealth, float maxHealth, float damage)
        {
            Enemy = enemy;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            Damage = damage;
        }
        
    }
}
