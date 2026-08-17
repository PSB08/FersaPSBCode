using System;
using System.Collections.Generic;
using YIS.Code.Skills;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Intents
{
    public readonly struct EnemyIntentProposal
    {
        private readonly Action _accepted;
        private readonly Action _fallbackAccepted;
        
        public EnemyMechanicSO Source { get; }
        public int Priority { get; }
        public EnemyIntentDecision Decision { get; }
        public EnemyIntentFallback Fallback { get; }
        public SkillDataSO Skill { get; }
        public IReadOnlyList<SkillDataSO> ExcludedSkills { get; }
        public string LogMessage { get; }
        
        public bool HasLog => !string.IsNullOrEmpty(LogMessage);
        
        private EnemyIntentProposal(EnemyMechanicSO source, int priority, EnemyIntentDecision decision,
            EnemyIntentFallback fallback, SkillDataSO skill, IReadOnlyList<SkillDataSO> excludedSkills,
            string logMessage, Action accepted, Action fallbackAccepted)
        {
            Source = source;
            Priority = priority;
            Decision = decision;
            Fallback = fallback;
            Skill = skill;
            ExcludedSkills = excludedSkills;
            LogMessage = logMessage;
            _accepted = accepted;
            _fallbackAccepted = fallbackAccepted;
        }
        
        public static EnemyIntentProposal SpecificSkill(EnemyMechanicSO source, int priority,
            SkillDataSO skill, string logMessage = null, Action accepted = null,
            EnemyIntentFallback fallback = EnemyIntentFallback.None,
            IReadOnlyList<SkillDataSO> excludedSkills = null)
        {
            return new EnemyIntentProposal(source, priority, EnemyIntentDecision.SpecificSkill,
                fallback, skill, excludedSkills, logMessage, accepted, null);
        }
        
        public static EnemyIntentProposal BestSkill(EnemyMechanicSO source, int priority,
            IReadOnlyList<SkillDataSO> excludedSkills = null, string logMessage = null,
            Action accepted = null, EnemyIntentFallback fallback = EnemyIntentFallback.None,
            Action fallbackAccepted = null)
        {
            return new EnemyIntentProposal(source, priority, EnemyIntentDecision.BestSkill,
                fallback, null, excludedSkills, logMessage, accepted, fallbackAccepted);
        }
        
        public static EnemyIntentProposal Skip(EnemyMechanicSO source, int priority,
            string logMessage = null, Action accepted = null)
        {
            return new EnemyIntentProposal(source, priority, EnemyIntentDecision.Skip,
                EnemyIntentFallback.None, null, null, logMessage, accepted, null);
        }
        
        public void Accept(EnemyIntentAcceptance acceptance)
        {
            if (acceptance == EnemyIntentAcceptance.FallbackAccepted)
                _fallbackAccepted?.Invoke();
            else if (acceptance == EnemyIntentAcceptance.Accepted)
                _accepted?.Invoke();
        }
        
        public EnemyIntentProposal WithExcludedSkills(IReadOnlyList<SkillDataSO> excludedSkills)
        {
            return new EnemyIntentProposal(Source, Priority, Decision, Fallback, Skill,
                excludedSkills, LogMessage, _accepted, _fallbackAccepted);
        }
        
    }
}
