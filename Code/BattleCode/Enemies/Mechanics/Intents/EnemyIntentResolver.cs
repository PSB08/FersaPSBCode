using System;
using System.Collections.Generic;
using YIS.Code.Skills;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Intents
{
    public sealed class EnemyIntentResolver
    {
        private sealed class Entry
        {
            public IEnemyIntentContributor Contributor;
            public int RegistrationOrder;
        }
        
        private readonly struct Candidate
        {
            public readonly EnemyIntentProposal Proposal;
            public readonly int RegistrationOrder;
            
            public Candidate(EnemyIntentProposal proposal, int registrationOrder)
            {
                Proposal = proposal;
                RegistrationOrder = registrationOrder;
            }
        }
        
        private readonly List<Entry> _entries = new();
        private readonly List<Candidate> _candidates = new();
        private readonly List<SkillDataSO> _excludedSkills = new();
        private int _nextRegistrationOrder;
        
        public IDisposable Register(IEnemyIntentContributor contributor)
        {
            if (contributor == null)
                return null;
            
            Entry entry = new Entry
            {
                Contributor = contributor,
                RegistrationOrder = _nextRegistrationOrder++
            };
            
            _entries.Add(entry);
            return new EnemyMechanicSubscription(() => _entries.Remove(entry));
        }
        
        public bool TryResolve(EnemyIntentRequest request,
            Func<EnemyIntentProposal, EnemyIntentAcceptance> tryAccept)
        {
            if (tryAccept == null)
                return false;
            
            _candidates.Clear();
            _excludedSkills.Clear();
            List<Entry> snapshot = new List<Entry>(_entries);
            
            for (int i = 0; i < snapshot.Count; i++)
            {
                Entry entry = snapshot[i];
                
                try
                {
                    if (entry.Contributor.TryProposeIntent(request, out EnemyIntentProposal proposal))
                    {
                        _candidates.Add(new Candidate(proposal, entry.RegistrationOrder));
                        CollectExcludedSkills(proposal.ExcludedSkills);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
            
            _candidates.Sort(CompareCandidates);
            SkillDataSO[] excludedSkills = _excludedSkills.ToArray();
            
            for (int i = 0; i < _candidates.Count; i++)
            {
                EnemyIntentProposal proposal =
                    _candidates[i].Proposal.WithExcludedSkills(excludedSkills);
                EnemyIntentAcceptance acceptance = tryAccept.Invoke(proposal);
                
                if (acceptance == EnemyIntentAcceptance.Rejected)
                    continue;
                
                try
                {
                    proposal.Accept(acceptance);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
                
                _candidates.Clear();
                _excludedSkills.Clear();
                return true;
            }
            
            _candidates.Clear();
            _excludedSkills.Clear();
            return false;
        }
        
        private void CollectExcludedSkills(IReadOnlyList<SkillDataSO> excludedSkills)
        {
            if (excludedSkills == null)
                return;
            
            for (int i = 0; i < excludedSkills.Count; i++)
            {
                SkillDataSO skill = excludedSkills[i];
                
                if (skill != null && !_excludedSkills.Contains(skill))
                    _excludedSkills.Add(skill);
            }
        }
        
        private static int CompareCandidates(Candidate left, Candidate right)
        {
            int priorityCompare = right.Proposal.Priority.CompareTo(left.Proposal.Priority);
            
            return priorityCompare != 0
                ? priorityCompare
                : left.RegistrationOrder.CompareTo(right.RegistrationOrder);
        }
        
    }
}
