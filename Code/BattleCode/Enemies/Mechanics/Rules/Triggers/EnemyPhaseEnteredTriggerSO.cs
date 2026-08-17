using System;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Triggers
{
    [CreateAssetMenu(fileName = "EnemyPhaseEnteredTrigger", menuName = "SO/Enemy/Mechanics/Triggers/PhaseEntered", order = 115)]
    public class EnemyPhaseEnteredTriggerSO : EnemyMechanicTriggerSO
    {
        [SerializeField, Min(1)] private int minimumPhaseNumber = 1;
        [SerializeField] private bool observeAllEnemies;
        
        public override EnemyMechanicTriggerRuntime CreateRuntime(
            EnemyMechanicContext context, Action<object> execute)
        {
            return new Runtime(context, execute, this);
        }
        
        private sealed class Runtime : EnemyMechanicTriggerRuntime
        {
            private readonly EnemyPhaseEnteredTriggerSO _so;
            
            public Runtime(EnemyMechanicContext context, Action<object> execute,
                EnemyPhaseEnteredTriggerSO so) : base(context, execute)
            {
                _so = so;
            }
            
            protected override void OnAttach()
            {
                if (_so.observeAllEnemies)
                    ListenBattle<EnemyPhaseEnteredSignal>(HandleSignal);
                else
                    Listen<EnemyPhaseEnteredSignal>(HandleSignal);
            }
            
            private void HandleSignal(EnemyPhaseEnteredSignal signal)
            {
                if (signal.PhaseNumber >= _so.minimumPhaseNumber)
                    Fire(signal);
            }
        }
        
    }
}
