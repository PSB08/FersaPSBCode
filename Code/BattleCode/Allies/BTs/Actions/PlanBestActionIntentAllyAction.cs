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
    [NodeDescription(name: "PlanBestActionIntentAlly", story: "plan best action intent on [Attack] in [SkillIndex]", category: "Action", id: "4b50af5975f2aa48aa7d51c4fd254309")]
    public partial class PlanBestActionIntentAllyAction : Action
    {
        [SerializeReference] public BlackboardVariable<AllyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;

        protected override Status OnStart()
        {
            return AllyPlanActionResult.Finish(Attack?.Value,
                Attack?.Value != null && Attack.Value.TryPlanBestActionIntent(), SkillIndex);
        }
    
    }
}

