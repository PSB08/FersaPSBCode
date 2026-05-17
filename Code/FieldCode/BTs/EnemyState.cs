using Unity.Behavior;

namespace Work.PSB.Code.FieldCode.BTs
{
    [BlackboardEnum]
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Hit,
        Attack
    }
}