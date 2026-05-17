namespace PSB.Code.BattleCode.Entities
{
    public interface IHealable
    {
        void HealFlat(float amount);
        void HealByCurrentPercent(float percent01);
        void HealByMaxPercent(float percent01);
    }
}