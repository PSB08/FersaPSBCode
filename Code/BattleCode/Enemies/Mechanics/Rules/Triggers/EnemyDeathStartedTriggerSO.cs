using System;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Triggers
{
    [CreateAssetMenu(fileName = "EnemyDeathStartedTrigger", menuName = "SO/Enemy/Mechanics/Triggers/DeathStarted", order = 117)]
    public class EnemyDeathStartedTriggerSO : EnemyMechanicTriggerSO
    {
        [SerializeField] private bool observeAllEnemies;
		
        public override EnemyMechanicTriggerRuntime CreateRuntime(EnemyMechanicContext context,
            Action<object> execute)
        {
            return new Runtime(context, execute, this);
        }
		
        private sealed class Runtime : EnemyMechanicTriggerRuntime
        {
            private readonly EnemyDeathStartedTriggerSO _so;
			
            public Runtime(EnemyMechanicContext context, Action<object> execute,
                EnemyDeathStartedTriggerSO so) : base(context, execute)
            {
                _so = so;
            }
			
            protected override void OnAttach()
            {
                if (_so.observeAllEnemies)
                    ListenBattle<EnemyDeathStartedSignal>(HandleSignal);
                else
                    Listen<EnemyDeathStartedSignal>(HandleSignal);
            }
			
            private void HandleSignal(EnemyDeathStartedSignal signal)
            {
                Fire(signal);
            }
			
        }
		
    }
	
}
