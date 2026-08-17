using System;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Triggers
{
    [CreateAssetMenu(fileName = "EnemyAttachedTrigger", menuName = "SO/Enemy/Mechanics/Triggers/Attached", order = 110)]
    public class EnemyAttachedTriggerSO : EnemyMechanicTriggerSO
    {
        public override EnemyMechanicTriggerRuntime CreateRuntime(
            EnemyMechanicContext context, Action<object> execute)
        {
            return new Runtime(context, execute);
        }
        
        private sealed class Runtime : EnemyMechanicTriggerRuntime
        {
            public Runtime(EnemyMechanicContext context, Action<object> execute) : base(context, execute)
            {
            }
            
            protected override void OnAttach()
            {
                Fire(null);
            }
        }
        
    }
}
