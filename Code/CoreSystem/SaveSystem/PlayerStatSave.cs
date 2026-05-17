using System;
using System.Collections.Generic;
using UnityEngine;

namespace PSB.Code.CoreSystem.SaveSystem
{
    [Serializable]
    public class StatSnapshot
    {
        public string statName;
        public float baseValue;
    }

    [Serializable]
    public class PlayerStatSaveData
    {
        public List<StatSnapshot> stats = new List<StatSnapshot>();
    }
    
    public static class PlayerStatSave
    {
        private const string Key = "PLAYER_STAT_SAVE";

        public static void Save(PlayerStatSaveData data)
        {
            if (data == null) return;
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        public static bool TryLoad(out PlayerStatSaveData data)
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                data = null;
                return false;
            }

            string json = PlayerPrefs.GetString(Key);
            if (string.IsNullOrEmpty(json))
            {
                data = null;
                return false;
            }

            data = JsonUtility.FromJson<PlayerStatSaveData>(json);
            return data != null;
        }

        public static void Reset()
        {
            if (!PlayerPrefs.HasKey(Key)) return;
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
        
    }
}