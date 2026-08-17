using PSB.Code.BattleCode.Allies.AttackCode;
using PSB.Code.BattleCode.Allies.BTs;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlanCostRecoveryIntentAlly", story: "plan cost recovery intent on [Attack] in [SkillIndex]", category: "Action", id: "adf62fe71930d4153f5b8636fddcdc4d")]
    public partial class PlanCostRecoveryIntentAllyAction : Action
    {
        [SerializeReference] public BlackboardVariable<AllyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;

        protected override Status OnStart()
        {
            return AllyPlanActionResult.Finish(Attack?.Value,
                Attack?.Value != null && Attack.Value.TryPlanCostRecoveryIntent(), SkillIndex);
        }
        
    }
}

