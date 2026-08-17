using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules
{
    public abstract class EnemyMechanicActionSO : ScriptableObject
    {
        public abstract EnemyMechanicActionRuntime CreateRuntime(EnemyMechanicContext context);
        
        public virtual void Validate(EnemySO enemySO, EnemyMechanicValidationReport report,
            string ownerName)
        {
        }
        
    }
}
