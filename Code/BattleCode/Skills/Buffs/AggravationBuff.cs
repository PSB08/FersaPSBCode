using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Skills.Buffs
{
    public class AggravationBuff : Buff
    {
        protected override void ApplyBuff(float value, int duration)
        {
            PlayEffect();
        }
        
        protected override void RemoveBuff()
        {
            StopEffect();
        }
        
    }
}
