using UnityEngine;

namespace Work.PSB.Code.RunSystem
{
    [CreateAssetMenu(fileName = "RunEventChance", menuName = "SO/Run/Event Action/Chance")]
    public class RunEventChanceSO : RunEventActionSO
    {
        [Range(0f, 1f)] public float successChance = 0.5f;
        public RunEventOutcomeData successOutcome = new RunEventOutcomeData();
        public RunEventOutcomeData failureOutcome = new RunEventOutcomeData();
    }
}
