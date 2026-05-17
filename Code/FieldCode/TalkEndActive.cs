using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using UnityEngine;
using Work.PSB.Code.FieldCode.MapSaves;

namespace Work.PSB.Code.FieldCode
{
    public class TalkEndActive : MonoBehaviour
    {
        [SerializeField] private NormalPortalEntity normalPortalEntity;
        
        private void OnEnable()
        {
            Bus<TalkFinished>.OnEvent += HandleTalkEnd;
        }

        private void OnDisable()
        {
            Bus<TalkFinished>.OnEvent -= HandleTalkEnd;
        }

        private void HandleTalkEnd(TalkFinished evt)
        {
            normalPortalEntity.Open();
        }
        
    }
}