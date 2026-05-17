using System.Collections.Generic;

namespace PSB.Code.CoreSystem.SaveSystem
{
    public static class BattleContext
    {
        public static bool HasContext { get; private set; }
        public static string FieldSceneName { get; private set; }
        public static List<string> InvolvedEnemyIDs { get; private set; } = new List<string>();
        public static bool ExitBySetting { get; private set; }

        public static void Set(string fieldSceneName, List<string> involvedEnemyIDs)
        {
            HasContext = true;
            FieldSceneName = fieldSceneName;
            InvolvedEnemyIDs = involvedEnemyIDs;
            ExitBySetting = false;
        }

        public static void Clear()
        {
            HasContext = false;
            FieldSceneName = null;
            InvolvedEnemyIDs.Clear();
            ExitBySetting = false;
        }
        
    }
}