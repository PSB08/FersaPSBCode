using System;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Triggers
{
    [CreateAssetMenu(fileName = "EnemyDeathAnimationEndedTrigger", menuName = "SO/Enemy/Mechanics/Triggers/DeathAnimationEnded", order = 118)]
    public class EnemyDeathAnimationEndedTriggerSO : EnemyMechanicTriggerSO
    {
        [SerializeField] private bool observeAllEnemies;
		
        public override EnemyMechanicTriggerRuntime CreateRuntime(EnemyMechanicContext context,
            Action<object> execute)
        {
            return new Runtime(context, execute, this);
        }
		
        private sealed class Runtime : EnemyMechanicTriggerRuntime
        {
            private readonly EnemyDeathAnimationEndedTriggerSO _so;
			
            public Runtime(EnemyMechanicContext context, Action<object> execute,
                EnemyDeathAnimationEndedTriggerSO so) : base(context, execute)
            {
                _so = so;
            }
			
            protected override void OnAttach()
            {
                if (_so.observeAllEnemies)
                    ListenBattle<EnemyDeathAnimationEndedSignal>(HandleSignal);
                else
                    Listen<EnemyDeathAnimationEndedSignal>(HandleSignal);
            }
			
            private void HandleSignal(EnemyDeathAnimationEndedSignal signal)
            {
                Fire(signal);
            }
			
        }
		
    }
	
}
