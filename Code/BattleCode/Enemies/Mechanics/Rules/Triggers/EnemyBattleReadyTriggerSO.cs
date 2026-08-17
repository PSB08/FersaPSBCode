using System;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Triggers
{
    [CreateAssetMenu(fileName = "EnemyBattleReadyTrigger", menuName = "SO/Enemy/Mechanics/Triggers/BattleReady", order = 110)]
    public class EnemyBattleReadyTriggerSO : EnemyMechanicTriggerSO
    {
        [SerializeField] private bool observeAllEnemies;
        
        public override EnemyMechanicTriggerRuntime CreateRuntime(
            EnemyMechanicContext context, Action<object> execute)
        {
            return new Runtime(context, execute, this);
        }
        
        private sealed class Runtime : EnemyMechanicTriggerRuntime
        {
            private readonly EnemyBattleReadyTriggerSO _so;
            private bool _fired;
            
            public Runtime(EnemyMechanicContext context, Action<object> execute,
                EnemyBattleReadyTriggerSO so) : base(context, execute)
            {
                _so = so;
            }
            
            protected override void OnAttach()
            {
                if (_so.observeAllEnemies)
                    ListenBattle<EnemyBattleReadySignal>(HandleSignal);
                else
                    Listen<EnemyBattleReadySignal>(HandleSignal);
            }
            
            private void HandleSignal(EnemyBattleReadySignal signal)
            {
                if (_fired)
                    return;
                
                _fired = true;
                Fire(signal);
            }
        }
        
    }
}
