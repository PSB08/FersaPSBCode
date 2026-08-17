using PSB.Code.BattleCode.BattleSystems.BattlePhases;
using PSB.Code.BattleCode.Enemies;

namespace Work.PSB.Code.FieldCode
{
    public static class BattleRuntimeData
    {
        public static BattleEncounterSO EncounterData { get; private set; }
        public static BattlePresentationSO Presentation { get; private set; }
        
        public static void Set(BattleEncounterSO encounter, BattlePresentationSO presentation)
        {
            EncounterData = encounter;
            Presentation = presentation;
        }

        public static EnemySO[] GetCurrentEnemies()
        {
            if (EncounterData == null || EncounterData.enemies == null || EncounterData.enemies.Length == 0)
                return null;
            return EncounterData.enemies;
        }

        public static void Clear()
        {
            EncounterData = null;
            Presentation = null;
        }
        
    }
}
