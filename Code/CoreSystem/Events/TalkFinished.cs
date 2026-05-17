using PSW.Code.EventBus;
using UnityEngine;

namespace PSB.Code.CoreSystem.Events
{
    public struct TalkFinished : IEvent
    {
        public string TalkId;
        public string EnemyId;
        public string RewardKey;

        public Vector3 WorldPos;

        public TalkFinished(string talkId, string enemyId, string rewardKey, Vector3 worldPos)
        {
            TalkId = talkId;
            EnemyId = enemyId;
            RewardKey = rewardKey;
            WorldPos = worldPos;
        }
        
    }
}