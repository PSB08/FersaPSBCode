using PSB.Code.BattleCode.Enemies.Mechanics.Intents;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Patterns
{
    [CreateAssetMenu(fileName = "EnemyWaitThenSkillMechanic", menuName = "SO/Enemy/Mechanics/Patterns/WaitThenSkill", order = 152)]
    public class EnemyWaitThenSkillMechanicSO : EnemyMechanicSO
    {
        [SerializeField] private SkillDataSO burstSkill;
        [SerializeField, Min(0)] private int waitTurnCount = 2;
        [SerializeField] private bool useOnlyOnce;
        
        public override EnemyMechanicRuntime CreateRuntime(EnemyMechanicContext context)
        {
            return new Runtime(context, this);
        }
        
        public override void Validate(EnemySO enemySO, EnemyMechanicValidationReport report)
        {
            if (burstSkill == null)
                report.AddError($"{name}에 발동할 스킬이 없습니다.");
            else if (!ContainsEnemySkill(enemySO, burstSkill))
                report.AddError($"{name}의 발동 스킬이 대상 EnemySO의 Attack Skills에 없습니다.");
            
            if (waitTurnCount < 0)
                report.AddError($"{name}의 Wait Turn Count는 0 이상이어야 합니다.");
        }
        
        private sealed class Runtime : EnemyMechanicRuntime, IEnemyIntentContributor
        {
            private readonly EnemyWaitThenSkillMechanicSO _so;
            
            private int _turnCount;
            private bool _consumed;
            
            public Runtime(EnemyMechanicContext context,
                EnemyWaitThenSkillMechanicSO so) : base(context, so)
            {
                _so = so;
            }
            
            protected override void OnAttach()
            {
                Listen<EnemyTurnStartedSignal>(HandleTurnStarted);
            }
            
            public bool TryProposeIntent(EnemyIntentRequest request,
                out EnemyIntentProposal proposal)
            {
                proposal = default;
                
                if (_consumed || _so.burstSkill == null)
                    return false;
                
                int waitCount = Mathf.Max(0, _so.waitTurnCount);
                int cycle = waitCount + 1;
                int currentTurn = Mathf.Max(1, _turnCount);
                int turnInCycle = (currentTurn - 1) % cycle;
                
                if (turnInCycle < waitCount)
                {
                    int remain = waitCount - turnInCycle;
                    
                    proposal = EnemyIntentProposal.Skip(_so, _so.Priority,
                        $"강한 공격을 준비합니다. {remain}턴 남았습니다.");
                    
                    return true;
                }
                
                proposal = EnemyIntentProposal.SpecificSkill(_so, _so.Priority,
                    _so.burstSkill, "준비한 강한 공격을 발동합니다.",
                    HandleBurstAccepted);
                
                return true;
            }
            
            private void HandleTurnStarted(EnemyTurnStartedSignal signal)
            {
                if (!signal.IsPlayerTurn)
                    _turnCount++;
            }
            
            private void HandleBurstAccepted()
            {
                if (_so.useOnlyOnce)
                    _consumed = true;
            }
        }
        
    }
}
