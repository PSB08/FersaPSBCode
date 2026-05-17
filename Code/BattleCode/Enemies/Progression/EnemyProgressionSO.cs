using System;
using PSB_Lib.StatSystem;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    [CreateAssetMenu(fileName = "EnemyProgressionSO", menuName = "SO/Enemy/ProgressionSO", order = 120)]
    public class EnemyProgressionSO : ScriptableObject
    {
        //강화 비율용 so
        [Serializable]
        public struct StatScale
        {
            public StatSO stat;
            public AnimationCurve multiplierByStage;
        }

        public StatScale[] scales;

        public float GetMultiplier(StatSO stat, int stage)
        {
            if (stat == null) return 1f;
            if (scales == null) return 1f;

            for (int i = 0; i < scales.Length; i++)
            {
                var s = scales[i].stat;
                if (s == null) continue;

                if (s.statName == stat.statName)
                {
                    var curve = scales[i].multiplierByStage;
                    if (curve == null) return 1f;

                    float m = curve.Evaluate(stage);
                    return Mathf.Max(0f, m);
                }
            }

            return 1f;
        }
        
    }
}
