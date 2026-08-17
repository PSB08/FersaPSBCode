namespace PSB.Code.BattleCode.Enemies.Mechanics.Signals
{
    public readonly struct EnemyHealthChangedSignal : IEnemyMechanicSignal
    {
        public BattleEnemy Enemy { get; }
        public float PreviousHealth { get; }
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
        
        public float HealthRatio => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
        
        public EnemyHealthChangedSignal(BattleEnemy enemy, float previousHealth,
            float currentHealth, float maxHealth)
        {
            Enemy = enemy;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }
        
    }
}
