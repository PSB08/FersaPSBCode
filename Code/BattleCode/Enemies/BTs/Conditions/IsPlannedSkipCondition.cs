using System;
using PSB.Code.BattleCode.Enemies.AttackCode;
using Unity.Behavior;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.BTs.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsPlannedSkipCondition", story: "[Attack] has planned skip", category: "Conditions", id: "9c28040d7b634b61d98641735b0e5f65")]
    public partial class IsPlannedSkipCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;

        public override bool IsTrue()
        {
            return Attack?.Value != null && Attack.Value.HasPlannedSkip;
        }
        
    }
}
