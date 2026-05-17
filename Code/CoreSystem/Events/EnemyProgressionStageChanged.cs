using PSW.Code.EventBus;

namespace PSB.Code.CoreSystem.Events
{
    public struct EnemyProgressionStageChanged : IEvent
    {
        public int stage;

        public EnemyProgressionStageChanged(int stage)
        {
            this.stage = stage;
        }
        
    }
}