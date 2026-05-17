using System.Collections.Generic;
using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using UnityEngine.SceneManagement;

namespace Work.PSB.Code.FieldCode.MapSaves
{
    public static class SceneLoader
    {
        private static int _pendingRevealCount;
        
        public static void MarkPendingReveal()
        {
            _pendingRevealCount++;
        }

        public static bool ConsumePendingReveal()
        {
            if (_pendingRevealCount <= 0) return false;
            _pendingRevealCount--;
            return true;
        }

        public static void LoadScene(string nextSceneName)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            bool isReloadSameScene = currentScene == nextSceneName;

            if (!isReloadSameScene)
            {
                SaveCurrentScene(currentScene);
            }
            else
            {
                SceneSaveSystem.DeleteSceneSave(currentScene);
            }

            Bus<SavePrefEvent>.Raise(
                new SavePrefEvent(() =>
                {
                    SceneObjectRegistry.Clear();
                    
                    MarkPendingReveal();

                    SceneManager.LoadScene(nextSceneName);
                })
            );
        }

        public static void LoadSceneAsync(string nextSceneName)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            bool isReloadSameScene = currentScene == nextSceneName;

            if (!isReloadSameScene)
            {
                SaveCurrentScene(currentScene);
            }
            else
            {
                SceneSaveSystem.DeleteSceneSave(currentScene);
            }

            Bus<SavePrefEvent>.Raise(
                new SavePrefEvent(() =>
                {
                    SceneObjectRegistry.Clear();

                    MarkPendingReveal();

                    SceneManager.LoadSceneAsync(nextSceneName);
                })
            );
        }

        public static void SaveCurrentScene(string sceneName)
        {
            SceneState data = SceneSaveSystem.LoadScene(sceneName) ?? new SceneState();

            PlayerStateHandler player = SceneObjectRegistry.GetPlayer();
            if (player != null) player.SavePlayer(ref data);

            data.enemies ??= new List<EnemySaveState>();
            data.enemies.Clear();
            var enemies = SceneObjectRegistry.GetEnemies();
            
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                
                if (e == null) 
                    continue;
                
                data.enemies.Add(new EnemySaveState
                {
                    id = e.EnemyID, 
                    isAlive = e.IsAlive, 
                    position = e.transform.position
                });
            }

            data.gimmicks ??= new List<GimmickSaveState>();
            data.gimmicks.Clear();
            var gimmicks = SceneObjectRegistry.GetGimmicks();
            
            for (int i = 0; i < gimmicks.Count; i++)
            {
                var g = gimmicks[i];
                
                if (g == null || string.IsNullOrEmpty(g.GimmickId)) 
                    continue;
                
                data.gimmicks.Add(new GimmickSaveState
                {
                    id = g.GimmickId, 
                    isCleared = g.isCleared
                });
            }

            data.talks ??= new List<TalkSaveState>();
            data.talks.Clear();
            var talks = SceneObjectRegistry.GetTalks();
            
            for (int i = 0; i < talks.Count; i++)
            {
                var t = talks[i];
                
                if (t == null || string.IsNullOrEmpty(t.TalkId))
                    continue;
                
                data.talks.Add(new TalkSaveState
                {
                    id = t.TalkId,
                    isFinished = t.IsFinished
                });
            }

            SceneSaveSystem.SaveScene(sceneName, data);
        }
        
    }
}