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
    [NodeDescription(name: "PlanDrawIntentAlly", story: "plan draw intent on [Attack] in [SkillIndex]", category: "Action", id: "640663a34e8120b456857b941dc672f6")]
    public partial class PlanDrawIntentAllyAction : Action
    {
        [SerializeReference] public BlackboardVariable<AllyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;

        protected override Status OnStart()
        {
            return AllyPlanActionResult.Finish(Attack?.Value,
                Attack?.Value != null && Attack.Value.TryPlanDrawIntent(), SkillIndex);
        }
        
    }
}

