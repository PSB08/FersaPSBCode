using System;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Triggers
{
    [CreateAssetMenu(fileName = "EnemyDamagedTrigger", menuName = "SO/Enemy/Mechanics/Triggers/Damaged", order = 113)]
    public class EnemyDamagedTriggerSO : EnemyMechanicTriggerSO
    {
        [SerializeField, Min(0f)] private float minimumDamage;
        [SerializeField] private bool observeAllEnemies;
        
        public override EnemyMechanicTriggerRuntime CreateRuntime(
            EnemyMechanicContext context, Action<object> execute)
        {
            return new Runtime(context, execute, this);
        }
        
        private sealed class Runtime : EnemyMechanicTriggerRuntime
        {
            private readonly EnemyDamagedTriggerSO _so;
            
            public Runtime(EnemyMechanicContext context, Action<object> execute,
                EnemyDamagedTriggerSO so) : base(context, execute)
            {
                _so = so;
            }
            
            protected override void OnAttach()
            {
                if (_so.observeAllEnemies)
                    ListenBattle<EnemyDamagedSignal>(HandleSignal);
                else
                    Listen<EnemyDamagedSignal>(HandleSignal);
            }
            
            private void HandleSignal(EnemyDamagedSignal signal)
            {
                if (signal.Damage >= _so.minimumDamage)
                    Fire(signal);
            }
        }
        
    }
}
