using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Entities;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Conditions
{
    [CreateAssetMenu(fileName = "EnemyHealthPercentCondition", menuName = "SO/Enemy/Mechanics/Conditions/HealthPercent", order = 120)]
    public class EnemyHealthPercentConditionSO : EnemyMechanicConditionSO
    {
        [SerializeField, Range(0f, 1f)] private float minimumHealthRatio;
        [SerializeField, Range(0f, 1f)] private float maximumHealthRatio = 1f;
        [SerializeField] private bool useSignalEnemy;
        
        public override EnemyMechanicConditionRuntime CreateRuntime(EnemyMechanicContext context)
        {
            return new Runtime(context, this);
        }
        
        public override void Validate(EnemySO enemySO, EnemyMechanicValidationReport report,
            string ownerName)
        {
            if (minimumHealthRatio > maximumHealthRatio)
                report.AddError($"{ownerName}의 {name} 최소 체력 비율이 최대 체력 비율보다 큽니다.");
        }
        
        private sealed class Runtime : EnemyMechanicConditionRuntime
        {
            private readonly EnemyHealthPercentConditionSO _so;
            
            public Runtime(EnemyMechanicContext context,
                EnemyHealthPercentConditionSO so) : base(context)
            {
                _so = so;
            }
            
            public override bool IsMet(EnemyMechanicExecutionContext executionContext)
            {
                BattleEnemy enemy = _so.useSignalEnemy
                    ? executionContext.SignalEnemy
                    : Context.Enemy;
                
                EntityHealth health = enemy != null ? enemy.GetModule<EntityHealth>() : null;
                
                if (health == null || health.MaxHealth <= 0f)
                    return false;
                
                float ratio = health.CurrentHealth / health.MaxHealth;
                
                return ratio >= _so.minimumHealthRatio &&
                       ratio <= _so.maximumHealthRatio;
            }
        }
        
    }
}
