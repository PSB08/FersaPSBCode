using System;
using PSB.Code.BattleCode.Enemies.AttackCode;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "PlanNextIntentAction", story: "plan next [Attack] in [SkillIndex]", category: "Action", id: "cbc193ba094daf7938e0d5d78366f8b3")]
    public partial class PlanNextIntentAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;
        [SerializeReference] public BlackboardVariable<int> SkillIndex;

        protected override Status OnStart()
        {
            if (Attack?.Value == null) return Status.Failure;

            bool ok = Attack.Value.PlanNextIntent();
            SkillIndex.Value = ok ? Attack.Value.PlannedIndex : -1;

            return ok ? Status.Success : Status.Failure;
        }
        
    }
}

