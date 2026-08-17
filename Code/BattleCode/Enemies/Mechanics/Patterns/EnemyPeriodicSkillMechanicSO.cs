using PSB.Code.BattleCode.Enemies.Mechanics.Intents;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Patterns
{
    [CreateAssetMenu(fileName = "EnemyPeriodicSkillMechanic", menuName = "SO/Enemy/Mechanics/Patterns/PeriodicSkill", order = 150)]
    public class EnemyPeriodicSkillMechanicSO : EnemyMechanicSO
    {
        [SerializeField] private SkillDataSO strongSkill;
        [SerializeField, Min(1)] private int turnInterval = 3;
        [SerializeField] private bool excludeStrongSkillOutsidePattern = true;
        [SerializeField] private bool useOnlyOnce;
        
        public override EnemyMechanicRuntime CreateRuntime(EnemyMechanicContext context)
        {
            return new Runtime(context, this);
        }
        
        public override void Validate(EnemySO enemySO, EnemyMechanicValidationReport report)
        {
            if (strongSkill == null)
                report.AddError($"{name}에 발동할 스킬이 없습니다.");
            else if (!ContainsEnemySkill(enemySO, strongSkill))
                report.AddError($"{name}의 발동 스킬이 대상 EnemySO의 Attack Skills에 없습니다.");
            
            if (turnInterval <= 0)
                report.AddError($"{name}의 Turn Interval은 1 이상이어야 합니다.");
        }
        
        private sealed class Runtime : EnemyMechanicRuntime, IEnemyIntentContributor
        {
            private readonly EnemyPeriodicSkillMechanicSO _so;
            private readonly SkillDataSO[] _excludedSkills;
            
            private int _turnCount;
            private bool _consumed;
            
            public Runtime(EnemyMechanicContext context,
                EnemyPeriodicSkillMechanicSO so) : base(context, so)
            {
                _so = so;
                _excludedSkills = new[]
                {
                    so.strongSkill
                };
            }
            
            protected override void OnAttach()
            {
                Listen<EnemyTurnStartedSignal>(HandleTurnStarted);
            }
            
            public bool TryProposeIntent(EnemyIntentRequest request,
                out EnemyIntentProposal proposal)
            {
                proposal = default;
                
                if (_consumed || _so.strongSkill == null)
                    return false;
                
                int interval = Mathf.Max(1, _so.turnInterval);
                
                if (_turnCount > 0 && _turnCount % interval == 0)
                {
                    proposal = EnemyIntentProposal.SpecificSkill(_so, _so.Priority,
                        _so.strongSkill, $"{interval}턴 주기 스킬을 발동합니다.",
                        HandleStrongSkillAccepted, EnemyIntentFallback.None,
                        _so.excludeStrongSkillOutsidePattern ? _excludedSkills : null);
                    
                    return true;
                }
                
                if (!_so.excludeStrongSkillOutsidePattern)
                    return false;
                
                int remain = interval - (_turnCount % interval);
                
                proposal = EnemyIntentProposal.BestSkill(_so, _so.Priority,
                    _excludedSkills, $"강한 공격까지 {remain}턴 남았습니다.", null, EnemyIntentFallback.Skip);
                
                return true;
            }
            
            private void HandleTurnStarted(EnemyTurnStartedSignal signal)
            {
                if (!signal.IsPlayerTurn)
                    _turnCount++;
            }
            
            private void HandleStrongSkillAccepted()
            {
                if (_so.useOnlyOnce)
                    _consumed = true;
            }
        }
        
    }
}
