using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Work.PSB.Code.FieldCode.MapSaves
{
    public static class SceneSaveSystem
    {
        private const string Prefix = "PSB_";

        private static string GetPath(string sceneName)
            => Path.Combine(Application.persistentDataPath, Prefix + sceneName + ".json");

        private static string GetSaveFolder()
            => Application.persistentDataPath;

        public static void SaveScene(string sceneName, SceneState data)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            if (data == null)
                data = new SceneState();

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetPath(sceneName), json);
        }

        public static SceneState LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;

            string path = GetPath(sceneName);
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SceneState>(json);
        }

        public static void SetEnemyAlive(string sceneName, string enemyId, bool isAlive)
        {
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(enemyId))
                return;

            SceneState data = LoadScene(sceneName) ?? new SceneState();

            if (data.enemies == null)
                data.enemies = new List<EnemySaveState>();

            int idx = data.enemies.FindIndex(e => e.id == enemyId);

            if (idx < 0)
            {
                data.enemies.Add(new EnemySaveState
                {
                    id = enemyId,
                    isAlive = isAlive,
                    position = Vector3.zero
                });
            }
            else
            {
                var e = data.enemies[idx];
                e.isAlive = isAlive;
                data.enemies[idx] = e;
            }

            SaveScene(sceneName, data);

            Debug.Log($"[SceneSaveSystem] {Prefix}{sceneName} enemy({enemyId}) isAlive={isAlive}");
        }
        
        public static void SetBoxCollected(string sceneName, string boxId, bool isCollected)
        {
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(boxId))
                return;

            SceneState data = LoadScene(sceneName) ?? new SceneState();

            if (data.boxes == null)
                data.boxes = new List<BoxSaveState>();

            int idx = data.boxes.FindIndex(b => b.id == boxId);

            if (idx < 0)
            {
                data.boxes.Add(new BoxSaveState
                {
                    id = boxId,
                    isCollected = isCollected,
                    position = Vector3.zero
                });
            }
            else
            {
                var b = data.boxes[idx];
                b.isCollected = isCollected;
                data.boxes[idx] = b;
            }

            SaveScene(sceneName, data);
            Debug.Log($"[SceneSaveSystem] {sceneName} box({boxId}) collected={isCollected}");
        }
        
        public static void SetGimmickCleared(string sceneName, string gimmickId, bool isCleared)
        {
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(gimmickId)) return;
            SceneState data = LoadScene(sceneName) ?? new SceneState();
            data.gimmicks ??= new List<GimmickSaveState>();

            int idx = data.gimmicks.FindIndex(g => g.id == gimmickId);
            if (idx < 0) data.gimmicks.Add(new GimmickSaveState { id = gimmickId, isCleared = isCleared });
            else data.gimmicks[idx].isCleared = isCleared;

            SaveScene(sceneName, data);
        }
        
        public static void SetTalkFinished(string sceneName, string talkId, bool isFinished)
        {
            if (string.IsNullOrEmpty(sceneName) || string.IsNullOrEmpty(talkId)) 
                return;
            
            SceneState data = LoadScene(sceneName) ?? new SceneState();
            data.talks ??= new List<TalkSaveState>();

            int idx = data.talks.FindIndex(t => t.id == talkId);
            if (idx < 0)
            {
                data.talks.Add(new TalkSaveState
                {
                    id = talkId, 
                    isFinished = isFinished
                });
            }
            else
            {
                data.talks[idx].isFinished = isFinished;
            }

            SaveScene(sceneName, data);
        }
        
        public static void DeleteAllSaves()
        {
            string folder = GetSaveFolder();
            if (!Directory.Exists(folder)) return;

            string[] files = Directory.GetFiles(folder, $"{Prefix}*.json");

            foreach (string file in files)
            {
                File.Delete(file);
            }

            Debug.Log($"<color=blue>[SceneSaveSystem] Deleted {Prefix}*.json</color>");
        }
        
        public static void DeleteSceneSave(string sceneName)
        {
            string path = GetPath(sceneName);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SceneSaveSystem] Deleted {Prefix}{sceneName}.json");
            }
        }
        
    }
}
