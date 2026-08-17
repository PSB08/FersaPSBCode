using PSB.Code.BattleCode.Allies.AttackCode;
using System;
using Unity.Behavior;
using UnityEngine;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsAllySkip", story: "[Attack] has planned skip", category: "Conditions", id: "e6e764cd4950cc3323a20269d6365cef")]
    public partial class IsAllySkipCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AllyAttack> Attack;

        public override bool IsTrue()
        {
            return Attack?.Value != null && Attack.Value.HasPlannedSkip;
        }
        
    }
}
