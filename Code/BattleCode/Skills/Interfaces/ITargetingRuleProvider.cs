namespace PSB.Code.BattleCode.Skills.Interfaces
{
    public interface ITargetingRuleProvider
    {
        bool BlocksDirectTargeting { get; }
        bool BlocksRangeTargeting { get; }
    }
}