using PSW.Code.EventBus;
using PSW.Code.Talk;
using UnityEngine;

namespace Work.PSB.Code.FieldCode.MiniGames.MonsterHunt
{
    public class MinigameRewardManager : MonoBehaviour
    {
        [SerializeField] private TalkEntity fiftyBox;
        [SerializeField] private TalkEntity seventyFiveBox;
        [SerializeField] private TalkEntity ninetyBox;
        
        private void OnEnable()
        {
            Bus<MonsterHuntReached50>.OnEvent += FiftyOn;
            Bus<MonsterHuntReached75>.OnEvent += SeventyFiveOn;
            Bus<MonsterHuntReached90>.OnEvent += NintyOn;
        }

        private void OnDisable()
        {
            Bus<MonsterHuntReached50>.OnEvent -= FiftyOn;
            Bus<MonsterHuntReached75>.OnEvent -= SeventyFiveOn;
            Bus<MonsterHuntReached90>.OnEvent -= NintyOn;
        }

        private void FiftyOn(MonsterHuntReached50 evt)
        {
            fiftyBox.gameObject.SetActive(true);
        }

        private void SeventyFiveOn(MonsterHuntReached75 evt)
        {
            seventyFiveBox.gameObject.SetActive(true);
        }
        
        private void NintyOn(MonsterHuntReached90 evt)
        {
            ninetyBox.gameObject.SetActive(true);
        }
        
    }
}