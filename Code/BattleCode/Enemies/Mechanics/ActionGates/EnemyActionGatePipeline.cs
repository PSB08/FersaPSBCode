using System;
using System.Collections;
using System.Collections.Generic;

namespace PSB.Code.BattleCode.Enemies.Mechanics.ActionGates
{
    public sealed class EnemyActionGatePipeline
    {
        private sealed class Entry
        {
            public IEnemyActionGate Gate;
            public int RegistrationOrder;
        }
        
        private readonly List<Entry> _entries = new();
        private int _nextRegistrationOrder;
        
        public IDisposable Register(IEnemyActionGate gate)
        {
            if (gate == null)
                return null;
            
            Entry entry = new Entry
            {
                Gate = gate,
                RegistrationOrder = _nextRegistrationOrder++
            };
            
            _entries.Add(entry);
            _entries.Sort(CompareEntries);
            
            return new EnemyMechanicSubscription(() => _entries.Remove(entry));
        }
        
        public IEnumerator ResolveBeforeAction(EnemyActionGateContext context)
        {
            List<Entry> snapshot = new List<Entry>(_entries);
            
            for (int i = 0; i < snapshot.Count; i++)
            {
                IEnemyActionGate gate = snapshot[i].Gate;
                
                if (gate == null || !gate.ShouldResolveBeforeAction(context))
                    continue;
                
                IEnumerator routine = gate.ResolveBeforeAction(context);
                
                if (routine != null)
                    yield return routine;
                
                if (context.Enemy == null || context.Enemy.IsDead)
                    yield break;
            }
        }
        
        private static int CompareEntries(Entry left, Entry right)
        {
            int priorityCompare = right.Gate.ActionGatePriority.CompareTo(
                left.Gate.ActionGatePriority);
            
            return priorityCompare != 0
                ? priorityCompare
                : left.RegistrationOrder.CompareTo(right.RegistrationOrder);
        }
        
    }
}
