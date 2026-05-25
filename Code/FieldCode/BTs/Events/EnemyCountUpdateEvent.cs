using PSW.Code.EventBus;
using UnityEngine;

namespace PSB.Code.FieldCode.BTs.Events
{
    public struct EnemyCountUpdateEvent : IEvent
    {
        public int AliveCount;
        public int TotalCount;

        public EnemyCountUpdateEvent(int aliveCount, int totalCount)
        {
            AliveCount = aliveCount;
            TotalCount = totalCount;
        }
        
    }
}