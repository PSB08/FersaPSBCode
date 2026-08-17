using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Targets
{
    [CreateAssetMenu(fileName = "EnemySignalTargetSelector", menuName = "SO/Enemy/Mechanics/Targets/SignalEnemy", order = 131)]
    public class EnemySignalTargetSelectorSO : EnemyMechanicTargetSelectorSO
    {
        public override void CollectTargets(EnemyMechanicExecutionContext executionContext,
            List<Entity> targets)
        {
            BattleEnemy enemy = executionContext.SignalEnemy;
            
            if (enemy != null && !enemy.IsDead)
                targets.Add(enemy);
        }
        
    }
}
