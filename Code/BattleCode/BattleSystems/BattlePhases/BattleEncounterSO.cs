using UnityEngine;
using PSB.Code.BattleCode.Enemies;

namespace PSB.Code.BattleCode.BattleSystems.BattlePhases
{
    [CreateAssetMenu(fileName = "NewBattleEncounter", menuName = "SO/Battle/BattleEncounter", order = 100)]
    public class BattleEncounterSO : ScriptableObject
    {
        public EnemySO[] enemies;
    }
}
