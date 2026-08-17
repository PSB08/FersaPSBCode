using PSB.Code.BattleCode.Enemies.Mechanics.Intents;
using PSB.Code.BattleCode.Enemies.Mechanics.Signals;
using UnityEngine;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Patterns
{
    [CreateAssetMenu(fileName = "EnemyHitCountSkillMechanic", menuName = "SO/Enemy/Mechanics/Patterns/HitCountSkill", order = 151)]
    public class EnemyHitCountSkillMechanicSO : EnemyMechanicSO
    {
        [SerializeField] private SkillDataSO strongSkill;
        [SerializeField, Min(1)] private int requiredHitCount = 3;
        [SerializeField] private bool resetCountOnUse = true;
        [SerializeField] private bool excludeStrongSkillBeforeReady = true;
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
            
            if (requiredHitCount <= 0)
                report.AddError($"{name}의 Required Hit Count는 1 이상이어야 합니다.");
        }
        
        private sealed class Runtime : EnemyMechanicRuntime, IEnemyIntentContributor
        {
            private readonly EnemyHitCountSkillMechanicSO _so;
            private readonly SkillDataSO[] _excludedSkills;
            
            private int _hitCount;
            private bool _consumed;
            
            public Runtime(EnemyMechanicContext context,
                EnemyHitCountSkillMechanicSO so) : base(context, so)
            {
                _so = so;
                _excludedSkills = new[]
                {
                    so.strongSkill
                };
            }
            
            protected override void OnAttach()
            {
                Listen<EnemyDamagedSignal>(HandleDamaged);
            }
            
            public bool TryProposeIntent(EnemyIntentRequest request,
                out EnemyIntentProposal proposal)
            {
                proposal = default;
                
                if (_consumed || _so.strongSkill == null)
                    return false;
                
                int required = Mathf.Max(1, _so.requiredHitCount);
                
                if (_hitCount >= required)
                {
                    proposal = EnemyIntentProposal.SpecificSkill(_so, _so.Priority,
                        _so.strongSkill, $"{required}회 피격 조건 스킬을 발동합니다.",
                        HandleStrongSkillAccepted, EnemyIntentFallback.None,
                        _so.excludeStrongSkillBeforeReady ? _excludedSkills : null);
                    
                    return true;
                }
                
                if (!_so.excludeStrongSkillBeforeReady)
                    return false;
                
                proposal = EnemyIntentProposal.BestSkill(_so, _so.Priority,
                    _excludedSkills, $"강한 공격까지 피격 {required - _hitCount}회 남았습니다.",
                    null, EnemyIntentFallback.Skip);
                
                return true;
            }
            
            private void HandleDamaged(EnemyDamagedSignal signal)
            {
                _hitCount++;
            }
            
            private void HandleStrongSkillAccepted()
            {
                int required = Mathf.Max(1, _so.requiredHitCount);
                
                if (_so.resetCountOnUse)
                    _hitCount = 0;
                else
                    _hitCount = Mathf.Max(0, _hitCount - required);
                
                if (_so.useOnlyOnce)
                    _consumed = true;
            }
        }
        
    }
}
