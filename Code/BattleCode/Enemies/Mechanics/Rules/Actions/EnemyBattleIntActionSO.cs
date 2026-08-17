using System.Collections.Generic;
using CIW.Code;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Actions
{
    [CreateAssetMenu(fileName = "EnemyBattleIntAction", menuName = "SO/Enemy/Mechanics/Actions/BattleInt", order = 143)]
    public class EnemyBattleIntActionSO : EnemyMechanicActionSO
    {
        [SerializeField] private string stateKey;
        [SerializeField] private EnemyBattleIntOperation operation;
        [SerializeField] private int value;
        
        public override EnemyMechanicActionRuntime CreateRuntime(EnemyMechanicContext context)
        {
            return new Runtime(context, this);
        }
        
        public override void Validate(EnemySO enemySO, EnemyMechanicValidationReport report,
            string ownerName)
        {
            if (string.IsNullOrWhiteSpace(stateKey))
                report.AddError($"{ownerName}의 {name}에 State Key가 없습니다.");
        }
        
        private sealed class Runtime : EnemyMechanicActionRuntime
        {
            private readonly EnemyBattleIntActionSO _so;
            
            public Runtime(EnemyMechanicContext context,
                EnemyBattleIntActionSO so) : base(context)
            {
                _so = so;
            }
            
            public override void Execute(EnemyMechanicExecutionContext executionContext,
                IReadOnlyList<Entity> targets)
            {
                EnemyBattleMechanicScope scope = Context.BattleScope;
                if (scope == null || string.IsNullOrEmpty(_so.stateKey)) return;
                
                int current = scope.GetState(_so.stateKey, 0);
                int result;
                
                switch (_so.operation)
                {
                    case EnemyBattleIntOperation.Set:
                        result = _so.value;
                        break;
                    case EnemyBattleIntOperation.Add:
                        result = current + _so.value;
                        break;
                    case EnemyBattleIntOperation.Minimum:
                        result = Mathf.Min(current, _so.value);
                        break;
                    case EnemyBattleIntOperation.Maximum:
                        result = Mathf.Max(current, _so.value);
                        break;
                    default:
                        result = current;
                        break;
                }
                
                scope.SetState(_so.stateKey, result);
            }
        }
        
    }
}
