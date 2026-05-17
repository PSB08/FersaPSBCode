using UnityEngine;

namespace Work.PSB.Code.FieldCode.MiniGames.MonsterHunt
{
    public class SceneStateRestorer_MonsterHunt : MonoBehaviour
    {
        [SerializeField] private FieldMonsterData[] monsters;

        [Header("MiniGame")]
        [SerializeField] private MonsterHuntMiniGameManager miniGameManager;

        private void Start()
        {
            if (monsters == null) return;

            foreach (var m in monsters)
            {
                if (m == null) continue;
                m.ForceHide();
            }
        }

        public void StartMiniGame()
        {
            if (monsters == null || miniGameManager == null) return;

            foreach (var m in monsters)
            {
                if (m == null) continue;
                m.ResetAndSpawn();
            }

            miniGameManager.StartGame(monsters);
        }
        
    }
}