using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules
{
    public abstract class EnemyMechanicConditionSO : ScriptableObject
    {
        public abstract EnemyMechanicConditionRuntime CreateRuntime(EnemyMechanicContext context);
        
        public virtual void Validate(EnemySO enemySO, EnemyMechanicValidationReport report,
            string ownerName)
        {
        }
        
    }
}
