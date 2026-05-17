using PSB.Code.BattleCode.Enemies;
using UnityEngine;

namespace Work.PSB.Code.FieldCode
{
    [CreateAssetMenu(fileName = "BattleDataSO", menuName = "SO/Data/BattleData", order = 10)]
    public class BattleDataSO : ScriptableObject
    {
        public EnemySO[] enemies;
    }
}