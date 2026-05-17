using PSB.Code.BattleCode.Entities;
using PSW.Code.EventBus;

namespace PSB.Code.BattleCode.Events
{
    public struct EnemyHealRequest : IEvent
    {
        public EntityHealth target;
        public float value;
        public HealMode mode;

        public EnemyHealRequest(EntityHealth target, float value, HealMode mode)
        {
            this.target = target;
            this.value = value;
            this.mode = mode;
        }
        //Bus<EnemyHealRequest>.Raise(new EnemyHealRequest(targetHealth, 0.2f, HealMode.MaxPercent));
    }
}