using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Conditions
{
    [CreateAssetMenu(fileName = "EnemyChanceCondition", menuName = "SO/Enemy/Mechanics/Conditions/Chance", order = 121)]
    public class EnemyChanceConditionSO : EnemyMechanicConditionSO
    {
        [SerializeField, Range(0f, 1f)] private float chance = 1f;
        
        public override EnemyMechanicConditionRuntime CreateRuntime(EnemyMechanicContext context)
        {
            return new Runtime(context, this);
        }
        
        private sealed class Runtime : EnemyMechanicConditionRuntime
        {
            private readonly EnemyChanceConditionSO _so;
            
            public Runtime(EnemyMechanicContext context, EnemyChanceConditionSO so) : base(context)
            {
                _so = so;
            }
            
            public override bool IsMet(EnemyMechanicExecutionContext executionContext)
            {
                return Random.value < Mathf.Clamp01(_so.chance);
            }
        }
        
    }
}
