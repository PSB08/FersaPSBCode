using PSB.Code.BattleCode.Allies.AttackCode;
using Unity.Behavior;

namespace PSB.Code.BattleCode.Allies.BTs
{
    public static class AllyPlanActionResult
    {
        public static Action.Status Finish(AllyAttack attack, bool success, BlackboardVariable<int> skillIndex)
        {
            if (skillIndex != null)
                skillIndex.Value = success && attack != null ? attack.PlannedIndex : -1;

            return success ? Action.Status.Success : Action.Status.Failure;
        }
    }
}