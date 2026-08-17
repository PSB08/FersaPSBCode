namespace PSB.Code.BattleCode.Enemies.Mechanics.Intents
{
    public readonly struct EnemyIntentRequest
    {
        public EnemyMechanicContext Context { get; }
        
        public EnemyIntentRequest(EnemyMechanicContext context)
        {
            Context = context;
        }
        
    }
}
