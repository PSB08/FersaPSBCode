using System;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Triggers
{
    [CreateAssetMenu(fileName = "EnemyDiedTrigger", menuName = "SO/Enemy/Mechanics/Triggers/Died", order = 116)]
    public class EnemyDiedTriggerSO : EnemyMechanicTriggerSO
    {
        [SerializeField] private bool observeAllEnemies;
        
        public override EnemyMechanicTriggerRuntime CreateRuntime(
            EnemyMechanicContext context, Action<object> execute)
        {
            return new Runtime(context, execute, this);
        }
        
        private sealed class Runtime : EnemyMechanicTriggerRuntime
        {
            private readonly EnemyDiedTriggerSO _so;
            
            public Runtime(EnemyMechanicContext context, Action<object> execute,
                EnemyDiedTriggerSO so) : base(context, execute)
            {
                _so = so;
            }
            
            protected override void OnAttach()
            {
                if (_so.observeAllEnemies)
                    ListenBattle<EnemyDiedSignal>(HandleSignal);
                else
                    Listen<EnemyDiedSignal>(HandleSignal);
            }
            
            private void HandleSignal(EnemyDiedSignal signal)
            {
                Fire(signal);
            }
        }
        
    }
}
