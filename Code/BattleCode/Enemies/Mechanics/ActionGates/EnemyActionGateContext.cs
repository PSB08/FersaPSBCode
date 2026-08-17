namespace PSB.Code.BattleCode.Enemies.Mechanics.ActionGates
{
    public readonly struct EnemyActionGateContext
    {
        public BattleEnemy Enemy { get; }
        public EnemyMechanicContext Mechanics { get; }
        
        public EnemyActionGateContext(BattleEnemy enemy, EnemyMechanicContext mechanics)
        {
            Enemy = enemy;
            Mechanics = mechanics;
        }
        
    }
}
