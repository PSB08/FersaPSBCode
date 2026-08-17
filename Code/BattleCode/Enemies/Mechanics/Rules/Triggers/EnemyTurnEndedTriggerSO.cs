using System;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Triggers
{
    [CreateAssetMenu(fileName = "EnemyTurnEndedTrigger", menuName = "SO/Enemy/Mechanics/Triggers/TurnEnded", order = 112)]
    public class EnemyTurnEndedTriggerSO : EnemyMechanicTriggerSO
    {
        [SerializeField] private bool triggerOnPlayerTurn;
        [SerializeField] private bool triggerOnEnemyTurn = true;
        [SerializeField] private bool observeAllEnemies;
        
        public override EnemyMechanicTriggerRuntime CreateRuntime(
            EnemyMechanicContext context, Action<object> execute)
        {
            return new Runtime(context, execute, this);
        }
        
        private sealed class Runtime : EnemyMechanicTriggerRuntime
        {
            private readonly EnemyTurnEndedTriggerSO _so;
            
            public Runtime(EnemyMechanicContext context, Action<object> execute,
                EnemyTurnEndedTriggerSO so) : base(context, execute)
            {
                _so = so;
            }
            
            protected override void OnAttach()
            {
                if (_so.observeAllEnemies)
                    ListenBattle<EnemyTurnEndedSignal>(HandleSignal);
                else
                    Listen<EnemyTurnEndedSignal>(HandleSignal);
            }
            
            private void HandleSignal(EnemyTurnEndedSignal signal)
            {
                if (signal.IsPlayerTurn && !_so.triggerOnPlayerTurn) return;
                if (!signal.IsPlayerTurn && !_so.triggerOnEnemyTurn) return;
                
                Fire(signal);
            }
        }
        
    }
}
