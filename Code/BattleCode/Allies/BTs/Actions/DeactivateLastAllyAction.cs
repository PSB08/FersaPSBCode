using PSB.Code.BattleCode.Allies.AttackCode;
using PSB.Code.BattleCode.Skills;
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "DeactivateLastAlly", story: "deactivate last skill on [Attack]", category: "Action", id: "1c85c20e71956102fb57d4e357a0cb21")]
    public partial class DeactivateLastAllyAction : Action
    {
        [SerializeReference] public BlackboardVariable<AllyAttack> Attack;
        
        protected override Status OnStart()
        {
            if (Attack?.Value == null) return Status.Failure;
            if (Attack.Value.SkillExecutor == null) return Status.Failure;
            
            if (Attack.Value.SkillExecutor?.LastUseResult == BtSkillUseResult.Success)
                Attack.Value.CompletePlannedIntent();
            
            Attack.Value.ClearIntent();
            return Status.Success;
        }
        
    }
}

