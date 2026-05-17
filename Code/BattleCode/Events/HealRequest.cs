using PSW.Code.EventBus;

namespace PSB.Code.BattleCode.Events
{
    public struct HealRequest : IEvent
    {
        public float value;
        public HealMode mode;

        public HealRequest(float value, HealMode mode)
        {
            this.value = value;
            this.mode = mode;
        }
        //Bus<HealRequest>.Raise(new HealRequest(0.2f, HealMode.MaxPercent));
    }
}