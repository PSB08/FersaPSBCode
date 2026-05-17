using Unity.Behavior;

namespace PSB.Code.BattleCode.Enemies
{
    [BlackboardEnum]
    public enum BattleEnemyState
    {
        Idle, 
        Move, 
        Attack, 
        Return, 
        Hit, 
        Dead
    }
}