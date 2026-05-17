using PSW.Code.EventBus;

namespace Work.PSB.Code.FieldCode.MiniGames.MonsterHunt
{
    public struct MonsterHuntReached50 : IEvent { }
    public struct MonsterHuntReached75 : IEvent { }
    public struct MonsterHuntReached90 : IEvent { }
    public struct MonsterHuntTimeOver : IEvent { }

    public struct MonsterHuntFinished : IEvent
    {
        public bool success;
        public int reachedTier;
        public int cleared;
        public int total;
    }
    
}