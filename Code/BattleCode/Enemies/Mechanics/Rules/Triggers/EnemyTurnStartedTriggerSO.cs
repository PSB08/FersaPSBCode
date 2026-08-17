using System;
using PSB.Code.BattleCode.Enemies.Mechanics.Rules;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules.Triggers
{
    [CreateAssetMenu(fileName = "EnemyTurnStartedTrigger", menuName = "SO/Enemy/Mechanics/Triggers/TurnStarted", order = 111)]
    public class EnemyTurnStartedTriggerSO : EnemyMechanicTriggerSO
    {
        [SerializeField] private bool triggerOnPlayerTurn;
        [SerializeField] private bool triggerOnEnemyTurn = true;
        [SerializeField] private bool observeAllEnemies;
        
        public override EnemyMechanicTriggerRuntime CreateRuntime(
            EnemyMechanicContext context, Action<object> execute)
        {
            return new Runtime(context, execute, this);
        }
        
        public override void Validate(EnemySO enemySO, EnemyMechanicValidationReport report,
            string ownerName)
        {
            if (!triggerOnPlayerTurn && !triggerOnEnemyTurn)
                report.AddError($"{ownerName}의 {name}에 활성화된 턴 종류가 없습니다.");
        }
        
        private sealed class Runtime : EnemyMechanicTriggerRuntime
        {
            private readonly EnemyTurnStartedTriggerSO _so;
            
            public Runtime(EnemyMechanicContext context, Action<object> execute,
                EnemyTurnStartedTriggerSO so) : base(context, execute)
            {
                _so = so;
            }
            
            protected override void OnAttach()
            {
                if (_so.observeAllEnemies)
                    ListenBattle<EnemyTurnStartedSignal>(HandleSignal);
                else
                    Listen<EnemyTurnStartedSignal>(HandleSignal);
            }
            
            private void HandleSignal(EnemyTurnStartedSignal signal)
            {
                if (signal.IsPlayerTurn && !_so.triggerOnPlayerTurn) return;
                if (!signal.IsPlayerTurn && !_so.triggerOnEnemyTurn) return;
                
                Fire(signal);
            }
        }
        
    }
}
