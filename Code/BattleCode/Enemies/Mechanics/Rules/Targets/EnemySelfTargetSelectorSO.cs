using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Targets
{
    [CreateAssetMenu(fileName = "EnemySelfTargetSelector", menuName = "SO/Enemy/Mechanics/Targets/Self", order = 130)]
    public class EnemySelfTargetSelectorSO : EnemyMechanicTargetSelectorSO
    {
        public override void CollectTargets(EnemyMechanicExecutionContext executionContext,
            List<Entity> targets)
        {
            BattleEnemy enemy = executionContext.Mechanics.Enemy;
            
            if (enemy != null && !enemy.IsDead)
                targets.Add(enemy);
        }
        
    }
}
