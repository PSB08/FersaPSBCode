using PSB.Code.BattleCode.BattleSystems.BattlePhases;
using PSB.Code.BattleCode.Enemies;

namespace Work.PSB.Code.FieldCode
{
    public static class BattleRuntimeData
    {
        public static BattleEncounterSO EncounterData { get; private set; }
        public static BattlePresentationSO Presentation { get; private set; }
        
        public static int CurrentPhaseIndex { get; private set; }

        public static void Set(BattleEncounterSO encounter, BattlePresentationSO presentation)
        {
            EncounterData = encounter;
            Presentation = presentation;
            CurrentPhaseIndex = 0;
        }

        public static bool MoveToNextPhase()
        {
            if (EncounterData == null || CurrentPhaseIndex >= EncounterData.phases.Length - 1)
                return false; 
            
            CurrentPhaseIndex++;
            return true;
        }

        public static EnemySO[] GetCurrentPhaseEnemies()
        {
            if (EncounterData == null || EncounterData.phases.Length == 0) return null;
            return EncounterData.phases[CurrentPhaseIndex].enemies;
        }

        public static void Clear()
        {
            EncounterData = null;
            Presentation = null;
            CurrentPhaseIndex = 0;
        }
        
    }
}