namespace PSB.Code.BattleCode.Enemies.Mechanics.Phases
{
    public readonly struct EnemyPhaseTransitionRequest
    {
        public EnemyMechanicContext Context { get; }
        public EnemyPhaseDefinition Phase { get; }
        public int PreviousPhaseNumber { get; }
        public int NextPhaseNumber { get; }
        public int TotalPhaseCount { get; }
        
        public EnemyPhaseTransitionRequest(EnemyMechanicContext context, EnemyPhaseDefinition phase,
            int previousPhaseNumber, int nextPhaseNumber, int totalPhaseCount)
        {
            Context = context;
            Phase = phase;
            PreviousPhaseNumber = previousPhaseNumber;
            NextPhaseNumber = nextPhaseNumber;
            TotalPhaseCount = totalPhaseCount;
        }
        
    }
}
