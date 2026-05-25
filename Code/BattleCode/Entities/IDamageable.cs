using YIS.Code.Combat;

namespace PSB.Code.BattleCode.Entities
{
    public interface IDamageable
    {
        public void ApplyDamage(DamageData damage);
    }
}