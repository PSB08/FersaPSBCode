using PSB.Code.BattleCode.Allies.AttackCode;
using System;
using Unity.Behavior;
using UnityEngine;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsMeleeAlly", story: "enemy is melee [Attack]", category: "Conditions", id: "8d011542a8c224c77abeb2263c7e4dd0")]
    public partial class IsMeleeAllyCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<AllyAttack> Attack;

        public override bool IsTrue()
        {
            return Attack.Value.IsMelee;
        }
        
    }
}
