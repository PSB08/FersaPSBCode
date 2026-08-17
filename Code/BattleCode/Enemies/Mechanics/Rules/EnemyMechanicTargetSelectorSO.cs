using System.Collections.Generic;
using CIW.Code;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules
{
    public abstract class EnemyMechanicTargetSelectorSO : ScriptableObject
    {
        public abstract void CollectTargets(EnemyMechanicExecutionContext executionContext,
            List<Entity> targets);
    }
}
