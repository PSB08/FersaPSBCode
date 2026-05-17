using System;
using PSB.Code.BattleCode.Enemies.AttackCode;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "DeactivateLastSkill", story: "deactivate last skill on [Attack]", category: "Action", id: "e8d3764c3a0f5d226fb53378e35052a7")]
    public partial class DeactivateLastSkillAction : Action
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;

        protected override Status OnStart()
        {
            if (Attack?.Value == null) return Status.Failure;
            if (Attack.Value.SkillExecutor == null) return Status.Failure;

            Attack.Value.ClearIntent();
            Attack.Value.SkillExecutor.DeactivateLast();
            return Status.Success;
        }
        
    }
}

