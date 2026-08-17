namespace Work.PSB.Code.RunSystem
{
    public readonly struct RunEventActionResult
    {
        public bool Executed { get; }
        public bool IsSuccess { get; }
        public RunEventOutcomeData Outcome { get; }
        
        public RunEventActionResult(bool executed, bool isSuccess, RunEventOutcomeData outcome)
        {
            Executed = executed;
            IsSuccess = isSuccess;
            Outcome = outcome;
        }
        
        public static RunEventActionResult Cancelled()
        {
            return new RunEventActionResult(false, false, null);
        }
        
    }
}
