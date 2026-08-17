using UnityEngine;

namespace Work.PSB.Code.RunSystem
{
    [CreateAssetMenu(fileName = "RunEventTransaction", menuName = "SO/Run/Event Action/Transaction")]
    public class RunEventTransactionSO : RunEventActionSO
    {
        public RunEventSelectionSettings selection = new RunEventSelectionSettings();
        public RunEventOutcomeData successOutcome = new RunEventOutcomeData();
    }
}
