using System.Collections.Generic;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Enums;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Conditions
{
    [CreateAssetMenu(fileName = "EnemyCountCondition", menuName = "SO/Enemy/Mechanics/Conditions/EnemyCount", order = 122)]
    public class EnemyCountConditionSO : EnemyMechanicConditionSO
    {
        [SerializeField, Min(0)] private int minimumCount = 1;
        [SerializeField] private int maximumCount = -1;
        [SerializeField] private bool includeSelf = true;
        [SerializeField] private bool filterByGrade;
        [SerializeField] private EnemyGrade requiredGrade;
        
        public override EnemyMechanicConditionRuntime CreateRuntime(EnemyMechanicContext context)
        {
            return new Runtime(context, this);
        }
        
        public override void Validate(EnemySO enemySO, EnemyMechanicValidationReport report,
            string ownerName)
        {
            if (maximumCount >= 0 && minimumCount > maximumCount)
                report.AddError($"{ownerName}의 {name} 최소 개수가 최대 개수보다 큽니다.");
        }
        
        private sealed class Runtime : EnemyMechanicConditionRuntime
        {
            private readonly EnemyCountConditionSO _so;
            
            public Runtime(EnemyMechanicContext context, EnemyCountConditionSO so) : base(context)
            {
                _so = so;
            }
            
            public override bool IsMet(EnemyMechanicExecutionContext executionContext)
            {
                if (Context.EnemyManager == null)
                    return false;
                
                IReadOnlyList<BattleEnemy> enemies = Context.EnemyManager.GetEnemies();
                int count = 0;
                
                for (int i = 0; i < enemies.Count; i++)
                {
                    BattleEnemy enemy = enemies[i];
                    
                    if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                        continue;
                    
                    if (!_so.includeSelf && ReferenceEquals(enemy, Context.Enemy))
                        continue;
                    
                    if (_so.filterByGrade &&
                        (enemy.enemySO == null || enemy.enemySO.grade != _so.requiredGrade))
                    {
                        continue;
                    }
                    
                    count++;
                }
                
                if (count < _so.minimumCount)
                    return false;
                
                return _so.maximumCount < 0 || count <= _so.maximumCount;
            }
        }
        
    }
}
