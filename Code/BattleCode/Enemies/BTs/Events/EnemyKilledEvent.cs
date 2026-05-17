using System;
using PSB.Code.BattleCode.Enums;
using PSW.Code.EventBus;

namespace PSB.Code.BattleCode.Enemies.BTs.Events
{
    [Serializable]
    public struct EnemyIdentity
    {
        public string EnemyName;
        public EnemyGrade Grade;
    }
    
    public struct EnemyKilledEvent : IEvent
    {
        public EnemyIdentity Identity;
        
        public EnemyKilledEvent(EnemyIdentity identity)
        {
            Identity = identity;
        }
        
    }
}