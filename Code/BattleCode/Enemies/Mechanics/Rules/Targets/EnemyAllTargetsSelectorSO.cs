using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Targets
{
    [CreateAssetMenu(fileName = "EnemyAllTargetsSelector", menuName = "SO/Enemy/Mechanics/Targets/AllEnemies", order = 132)]
    public class EnemyAllTargetsSelectorSO : EnemyMechanicTargetSelectorSO
    {
        [SerializeField] private bool includeSelf = true;
        
        public override void CollectTargets(EnemyMechanicExecutionContext executionContext,
            List<Entity> targets)
        {
            BattleEnemyManager manager = executionContext.Mechanics.EnemyManager;
            
            if (manager == null)
                return;
            
            IReadOnlyList<BattleEnemy> enemies = manager.GetEnemies();
            
            for (int i = 0; i < enemies.Count; i++)
            {
                BattleEnemy enemy = enemies[i];
                
                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                    continue;
                
                if (!includeSelf && ReferenceEquals(enemy, executionContext.Mechanics.Enemy))
                    continue;
                
                targets.Add(enemy);
            }
        }
        
    }
}
