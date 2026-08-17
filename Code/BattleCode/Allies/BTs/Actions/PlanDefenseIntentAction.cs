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
    [NodeDescription(name: "PlanDefenseIntent", story: "plan defense intent on [Attack] in [SkillIndex]", category: "Action", id: "92f00047c060cede664934875eea81fe")]
    public partial class PlanDefenseIntentAction : Action
    {
        [SerializeReference] public BlackboardVariable<AllyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;

        protected override Status OnStart()
        {
            return AllyPlanActionResult.Finish(Attack?.Value,
                Attack?.Value != null && Attack.Value.TryPlanDefenseIntent(), SkillIndex);
        }
        
    }
}

