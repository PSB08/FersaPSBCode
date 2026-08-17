using Unity.Behavior;

namespace PSB.Code.BattleCode.Allies
{
    [BlackboardEnum]
    public enum BattleAllyState
    {
        Idle,
        Move,
        Attack,
        Return,
        Hit,
        Dead,
        Stun,
        Wake
    }
}
