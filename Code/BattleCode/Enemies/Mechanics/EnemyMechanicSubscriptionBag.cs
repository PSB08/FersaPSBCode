using System;
using System.Collections.Generic;

namespace PSB.Code.BattleCode.Enemies.Mechanics
{
    public sealed class EnemyMechanicSubscriptionBag : IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed;
        
        public void Add(IDisposable subscription)
        {
            if (subscription == null) return;
            
            if (_disposed)
            {
                subscription.Dispose();
                return;
            }
            
            _subscriptions.Add(subscription);
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            for (int i = _subscriptions.Count - 1; i >= 0; i--)
            {
                _subscriptions[i]?.Dispose();
            }
            
            _subscriptions.Clear();
        }
        
    }
}
