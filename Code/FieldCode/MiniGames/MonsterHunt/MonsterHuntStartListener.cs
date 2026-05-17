using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using UnityEngine;

namespace Work.PSB.Code.FieldCode.MiniGames.MonsterHunt
{
    public class MonsterHuntStartListener : MonoBehaviour
    {
        [SerializeField] private string startKey = "MINIGAME_MONSTERHUNT_START";
        [SerializeField] private SceneStateRestorer_MonsterHunt restorer;

        private void Awake()
        {
            Bus<TalkFinished>.OnEvent += OnTalkFinished;
        }

        private void OnDestroy()
        {
            Bus<TalkFinished>.OnEvent -= OnTalkFinished;
        }

        private void OnTalkFinished(TalkFinished evt)
        {
            if (evt.RewardKey != startKey && evt.TalkId != startKey) return;
            
            if (restorer == null)
            {
                Debug.LogError("[MonsterHuntStartListener] SceneStateRestorer_MonsterHunt not found.");
                return;
            }

            restorer.StartMiniGame();
        }
        
    }
}