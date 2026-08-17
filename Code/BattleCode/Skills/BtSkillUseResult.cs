namespace PSB.Code.BattleCode.Skills
{
    public enum BtSkillUseResult
    {
        Success,
        BlockedAfterTry,
        NoTarget,
        Failed
    }
    
    public static class SkillUseResultExtensions
    {
        public static bool WasTried(this BtSkillUseResult result)
        {
            return result == BtSkillUseResult.Success || result == BtSkillUseResult.BlockedAfterTry;
        }
        
        public static bool ShouldStopRemainingActions(this BtSkillUseResult result)
        {
            return result == BtSkillUseResult.BlockedAfterTry 
                   || result == BtSkillUseResult.NoTarget || result == BtSkillUseResult.Failed;
        }
    }
    
    public interface ISkillUseResultExecutor
    {
        BtSkillUseResult LastUseResult { get; }
    }
    
}
