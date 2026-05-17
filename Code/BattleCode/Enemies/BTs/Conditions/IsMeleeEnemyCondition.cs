using System;
using PSB.Code.BattleCode.Enemies.AttackCode;
using Unity.Behavior;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.BTs.Conditions
{
    [Serializable, Unity.Properties.GeneratePropertyBag]
    [Condition(name: "IsMeleeEnemy", story: "enemy is melee [Attack]", category: "Conditions", id: "4c85e485c424eda11bcbfc392c27fcac")]
    public partial class IsMeleeEnemyCondition : Condition
    {
        [SerializeReference] public BlackboardVariable<EnemyAttack> Attack;

        public override bool IsTrue()
        {
            return Attack.Value.IsMelee;
        }
       
    }
}
