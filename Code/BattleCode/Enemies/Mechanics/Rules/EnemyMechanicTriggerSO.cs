using System;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules
{
    public abstract class EnemyMechanicTriggerSO : ScriptableObject
    {
        public abstract EnemyMechanicTriggerRuntime CreateRuntime(
            EnemyMechanicContext context, Action<object> execute);
        
        public virtual void Validate(EnemySO enemySO, EnemyMechanicValidationReport report,
            string ownerName)
        {
        }
        
    }
}
