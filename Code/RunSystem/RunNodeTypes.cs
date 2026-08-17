namespace Work.PSB.Code.RunSystem
{
    public enum RunPhase
    {
        None,
        ReadyToStart,
        Running,
        WaitingNext,
        Finished
    }
    
    public enum RunNodeType
    {
        Battle,
        Event,
        Rest,
        Boss
    }
    
    public enum RunEventPhase
    {
        None,
        BeforeBattle,
        InBattle,
        AfterVictory
    }
    
    public enum RunEventBattleResult
    {
        None,
        Success,
        Failure
    }
    
    public enum RunEventBattleChallengeMode
    {
        None,
        DamageWithinTurns
    }
    
    public enum RunAdvanceResult
    {
        None,
        NextNode,
        NextChapter,
        Finished
    }
    
}
