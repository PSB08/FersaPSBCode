using PSW.Code.EventBus;
using UnityEngine;
using Work.PSB.Code.FieldCode.MapSaves;

namespace Work.PSB.Code.FieldCode.MiniGames.MonsterHunt
{
    public class MinigameEnd : MonoBehaviour
    {
        [SerializeField] private NormalPortalEntity normalPortalEntity;
        
        private void OnEnable()
        {
            Bus<MonsterHuntFinished>.OnEvent += HandleMiniEnd;
        }

        private void OnDisable()
        {
            Bus<MonsterHuntFinished>.OnEvent -= HandleMiniEnd;
        }

        private void HandleMiniEnd(MonsterHuntFinished evt)
        {
            normalPortalEntity.Open();
        }
        
    }
}