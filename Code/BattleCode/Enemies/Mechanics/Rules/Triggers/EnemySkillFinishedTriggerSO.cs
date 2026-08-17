using System;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using PSB.Code.BattleCode.Skills;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Triggers
{
    [CreateAssetMenu(fileName = "EnemySkillFinishedTrigger", menuName = "SO/Enemy/Mechanics/Triggers/SkillFinished", order = 114)]
    public class EnemySkillFinishedTriggerSO : EnemyMechanicTriggerSO
    {
        [SerializeField] private bool successOnly = true;
        [SerializeField] private bool observeAllEnemies;
        
        public override EnemyMechanicTriggerRuntime CreateRuntime(
            EnemyMechanicContext context, Action<object> execute)
        {
            return new Runtime(context, execute, this);
        }
        
        private sealed class Runtime : EnemyMechanicTriggerRuntime
        {
            private readonly EnemySkillFinishedTriggerSO _so;
            
            public Runtime(EnemyMechanicContext context, Action<object> execute,
                EnemySkillFinishedTriggerSO so) : base(context, execute)
            {
                _so = so;
            }
            
            protected override void OnAttach()
            {
                if (_so.observeAllEnemies)
                    ListenBattle<EnemySkillFinishedSignal>(HandleSignal);
                else
                    Listen<EnemySkillFinishedSignal>(HandleSignal);
            }
            
            private void HandleSignal(EnemySkillFinishedSignal signal)
            {
                if (_so.successOnly && signal.Result != BtSkillUseResult.Success)
                    return;
                
                Fire(signal);
            }
        }
        
    }
}
