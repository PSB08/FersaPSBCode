using UnityEngine;

namespace PSB.Code.BattleCode.BattleSystems.BattlePhases
{
    [CreateAssetMenu(fileName = "NewBattlePhaseData", menuName = "SO/Battle/BattlePhaseData", order = 100)]
    public class BattleEncounterSO : ScriptableObject
    {
        public BattlePhaseData[] phases;
    }
}