using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.AttackCode
{
    public static class EnemyProgressionService
    {
        //강화 저장용 코드 
        private const string SaveKey = "ENEMY_PROGRESSION_SAVE";

        public static int LoadStage(int defaultStage = 0)
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return defaultStage;

            string json = PlayerPrefs.GetString(SaveKey);
            var data = JsonUtility.FromJson<EnemyProgressionSaveData>(json);
            return data != null ? Mathf.Max(0, data.stage) : defaultStage;
        }

        public static void SaveStage(int stage)
        {
            var data = new EnemyProgressionSaveData { stage = Mathf.Max(0, stage) };
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SaveKey);
        }
        
    }
}