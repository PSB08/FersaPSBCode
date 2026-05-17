using System.Collections;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using PSW.Code.Talk;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Work.PSB.Code.FieldCode.Gimmicks;

namespace Work.PSB.Code.FieldCode.MapSaves
{
    public class SceneStateRestorer : MonoBehaviour
    {
        [SerializeField] private PlayerStateHandler playerHandler;
        [SerializeField] private FieldEnemyData[] enemyData;
        [SerializeField] private FieldBoxCollectible[] boxes;
        [SerializeField] private TalkEntity[] talkEntities;
        [SerializeField] private FieldGimmick[] gimmicks;
        
        private void OnEnable() => Bus<VillageResetEvent>.OnEvent += HandleVillageReset;
        private void OnDisable() => Bus<VillageResetEvent>.OnEvent -= HandleVillageReset;
        
        private void HandleVillageReset(VillageResetEvent evt)
        {
            if (playerHandler != null)
            {
                playerHandler.ResetToInitial();
                Debug.Log("<color=purple>마을 초기화 - 플레이어 위치 리셋</color>");
            }
        }

        private IEnumerator Start() 
        { 
            string sceneName = SceneManager.GetActiveScene().name; 
            var data = SceneSaveSystem.LoadScene(sceneName); 
            
            if (data == null)
            {
                if (playerHandler != null)
                    playerHandler.ResetToInitial();
                yield break;
            }
            
            if (playerHandler != null) 
                playerHandler.LoadPlayer(data); 
            
            foreach (var enemy in enemyData) 
                enemy.LoadEnemy(data); 
            
            if (gimmicks != null && data.gimmicks != null)
            {
                foreach (var gimmick in gimmicks)
                {
                    if (gimmick == null || string.IsNullOrEmpty(gimmick.GimmickId)) 
                        continue;
                    
                    bool isCleared = data.gimmicks.Exists(g => g.id == gimmick.GimmickId && g.isCleared);
                    
                    if (isCleared)
                    {
                        gimmick.RestoreClearedState();
                    }
                }
            }
            
            if (boxes != null && data.boxes != null)
            {
                foreach (var box in boxes)
                {
                    if (box == null) 
                        continue;

                    bool collected = data.boxes.Exists(b => b.id == box.BoxId && b.isCollected);
                    
                    if (collected)
                        box.gameObject.SetActive(false);
                }
            }
            
            if (talkEntities != null && data.talks != null)
            {
                foreach (var talk in talkEntities)
                {
                    if (talk == null || string.IsNullOrEmpty(talk.TalkId)) 
                        continue;
                    
                    bool isFinished = data.talks.Exists(t => t.id == talk.TalkId && t.isFinished);
                    
                    if (isFinished)
                    {
                        talk.IsFinished = true;
                        
                        if (talk.DisableObjectAfterFinished)
                        {
                            talk.gameObject.SetActive(false);
                        }
                        else if (talk.DisableTalkAfterFinished)
                        {
                            talk.DisableTalk();
                        }
                    }
                }
            }

            yield return null;
            CheckAllEnemiesDead(); 
        }

        private void CheckAllEnemiesDead()
        {
            if (enemyData == null || enemyData.Length == 0)
                return;

            foreach (var enemy in enemyData)
            {
                if (enemy != null && enemy.IsAlive)
                    return;
            }

            Bus<EnemyAllNotAlive>.Raise(new EnemyAllNotAlive());
        }

        #if  UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                SceneSaveSystem.DeleteAllSaves();
            }
        }
        #endif
        
    }
}