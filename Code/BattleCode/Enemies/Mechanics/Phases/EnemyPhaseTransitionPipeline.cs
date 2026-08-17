using System;
using System.Collections.Generic;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Phases
{
    public sealed class EnemyPhaseTransitionPipeline
    {
        private sealed class Entry
        {
            public IEnemyPhaseTransitionGuard Guard;
            public int RegistrationOrder;
        }
        
        private readonly List<Entry> _entries = new();
        private int _nextRegistrationOrder;
        
        public IDisposable Register(IEnemyPhaseTransitionGuard guard)
        {
            if (guard == null)
                return null;
            
            Entry entry = new Entry
            {
                Guard = guard,
                RegistrationOrder = _nextRegistrationOrder++
            };
            
            _entries.Add(entry);
            _entries.Sort(CompareEntries);
            
            return new EnemyMechanicSubscription(() => _entries.Remove(entry));
        }
        
        public void RequestTransition(EnemyPhaseTransitionRequest request, Action completeTransition)
        {
            List<Entry> snapshot = new List<Entry>(_entries);
            ContinueTransition(snapshot, 0, request, completeTransition);
        }
        
        private static void ContinueTransition(IReadOnlyList<Entry> entries, int index,
            EnemyPhaseTransitionRequest request, Action completeTransition)
        {
            if (index >= entries.Count)
            {
                completeTransition?.Invoke();
                return;
            }
            
            Entry entry = entries[index];
            bool continued = false;
            
            void ContinueOnce()
            {
                if (continued) return;
                
                continued = true;
                ContinueTransition(entries, index + 1, request, completeTransition);
            }
            
            try
            {
                bool held = entry.Guard.TryHoldTransition(request, ContinueOnce);
                
                if (!held)
                    ContinueOnce();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ContinueOnce();
            }
        }
        
        private static int CompareEntries(Entry left, Entry right)
        {
            int priorityCompare = right.Guard.PhaseTransitionPriority.CompareTo(
                left.Guard.PhaseTransitionPriority);
            
            return priorityCompare != 0
                ? priorityCompare
                : left.RegistrationOrder.CompareTo(right.RegistrationOrder);
        }
        
    }
}
