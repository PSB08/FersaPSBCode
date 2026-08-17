using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Conditions
{
    [CreateAssetMenu(fileName = "EnemyBattleIntCondition", menuName = "SO/Enemy/Mechanics/Conditions/BattleInt", order = 123)]
    public class EnemyBattleIntConditionSO : EnemyMechanicConditionSO
    {
        [SerializeField] private string stateKey;
        [SerializeField] private EnemyBattleIntComparison comparison;
        [SerializeField] private int value;
        
        public override EnemyMechanicConditionRuntime CreateRuntime(EnemyMechanicContext context)
        {
            return new Runtime(context, this);
        }
        
        public override void Validate(EnemySO enemySO, EnemyMechanicValidationReport report,
            string ownerName)
        {
            if (string.IsNullOrWhiteSpace(stateKey))
                report.AddError($"{ownerName}의 {name}에 State Key가 없습니다.");
        }
        
        private sealed class Runtime : EnemyMechanicConditionRuntime
        {
            private readonly EnemyBattleIntConditionSO _so;
            
            public Runtime(EnemyMechanicContext context, EnemyBattleIntConditionSO so) : base(context)
            {
                _so = so;
            }
            
            public override bool IsMet(EnemyMechanicExecutionContext executionContext)
            {
                int current = Context.BattleScope != null
                    ? Context.BattleScope.GetState(_so.stateKey, 0)
                    : 0;
                
                switch (_so.comparison)
                {
                    case EnemyBattleIntComparison.Equal:
                        return current == _so.value;
                    case EnemyBattleIntComparison.NotEqual:
                        return current != _so.value;
                    case EnemyBattleIntComparison.Greater:
                        return current > _so.value;
                    case EnemyBattleIntComparison.GreaterOrEqual:
                        return current >= _so.value;
                    case EnemyBattleIntComparison.Less:
                        return current < _so.value;
                    case EnemyBattleIntComparison.LessOrEqual:
                        return current <= _so.value;
                    default:
                        return false;
                }
            }
        }
        
    }
}
