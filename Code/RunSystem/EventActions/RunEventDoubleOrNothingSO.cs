using UnityEngine;
using YIS.Code.Defines;

namespace Work.PSB.Code.RunSystem
{
    public enum RunEventDoubleOrNothingOperation
    {
        Gamble,
        CashOut
    }
    
    [CreateAssetMenu(fileName = "RunEventDoubleOrNothing", menuName = "SO/Run/Event Action/Double Or Nothing")]
    public class RunEventDoubleOrNothingSO : RunEventActionSO
    {
        public RunEventDoubleOrNothingOperation operation;
        public string sharedStateKey;
        public ItemType currencyType = ItemType.Coin;
        [Min(1)] public int startingAmount = 1;
        [Range(0f, 1f)] public float successChance = 0.5f;
        [Min(2)] public int successMultiplier = 2;
        
        public RunEventOutcomeData successOutcome = new RunEventOutcomeData();
        public RunEventOutcomeData failureOutcome = new RunEventOutcomeData();
        
        public string StateKey => string.IsNullOrWhiteSpace(sharedStateKey) ? ActionId : sharedStateKey.Trim();
    }
}
