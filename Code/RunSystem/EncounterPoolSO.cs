using PSB.Code.BattleCode.BattleSystems.BattlePhases;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Work.PSB.Code.RunSystem
{
    [CreateAssetMenu(fileName = "EncounterPool", menuName = "SO/Run/EncounterPool")]
    public class EncounterPoolSO : ScriptableObject
    {
        [Header("Fallback Encounters")]
        public BattleEncounterSO[] normalEncounters;
        public BattleEncounterSO[] eliteMixedEncounters;
        public BattleEncounterSO[] bossEncounters;
        
        [Header("Elite Chance")]
        [Min(0)] public int eliteStartNodeIndex = 4;
        public AnimationCurve eliteChanceByProgress = AnimationCurve.Linear(0, 0, 1, 0.6f);
        
        public BattleEncounterSO PickBattle(int nodeIndex, int maxNodeIndex)
        {
            if (nodeIndex < eliteStartNodeIndex || eliteMixedEncounters == null || eliteMixedEncounters.Length == 0)
                return Pick(normalEncounters);
            
            float progress = Mathf.Clamp01(nodeIndex / (float)Mathf.Max(1, maxNodeIndex));
            bool useElite = Random.value < eliteChanceByProgress.Evaluate(progress);
            return Pick(useElite ? eliteMixedEncounters : normalEncounters);
        }
        
        public BattleEncounterSO PickBoss()
        {
            return Pick(bossEncounters);
        }
        
        private BattleEncounterSO Pick(BattleEncounterSO[] list)
        {
            if (list == null || list.Length == 0)
                return null;
            
            int validCount = 0;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] != null)
                    validCount++;
            }
            
            if (validCount <= 0)
                return null;
            
            int targetIndex = Random.Range(0, validCount);
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] == null)
                    continue;
                
                if (targetIndex == 0)
                    return list[i];
                
                targetIndex--;
            }
            
            return null;
        }
        
    }
}
