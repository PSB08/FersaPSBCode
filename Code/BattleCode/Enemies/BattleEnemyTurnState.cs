using Unity.Behavior;

namespace PSB.Code.BattleCode.Enemies
{
    [BlackboardEnum]
    public enum BattleEnemyTurnState
    {
        Idle = 0,
        PlanAction = 1,
        PrepareTurn = 2,
        DecideDefense = 3,
        UseDefenseSkill = 4,
        UseDrawSkill = 5,
        UseCostRecoverySkill = 6,
        SelectBestAction = 7,
        UseBasicAttack = 8,
        UseSkill = 9,
        UseChainSkill = 10,
        UseGimmick = 11,
        EndTurn = 12,
        SkipTurn = 13,
        Finished = 14
    }
}
