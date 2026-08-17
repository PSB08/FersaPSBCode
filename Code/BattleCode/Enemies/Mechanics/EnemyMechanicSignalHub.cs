using System;
using System.Collections.Generic;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.Mechanics
{
    public sealed class EnemyMechanicSignalHub
    {
        private readonly Dictionary<Type, Delegate> _listeners = new();
        
        public IDisposable Subscribe<TSignal>(Action<TSignal> listener)
        {
            if (listener == null)
                return null;
            
            Type type = typeof(TSignal);
            
            if (_listeners.TryGetValue(type, out Delegate current))
                _listeners[type] = Delegate.Combine(current, listener);
            else
                _listeners[type] = listener;
            
            return new EnemyMechanicSubscription(() => Unsubscribe(listener));
        }
        
        public void Publish<TSignal>(TSignal signal)
        {
            if (!_listeners.TryGetValue(typeof(TSignal), out Delegate current))
                return;
            
            Action<TSignal> callback = current as Action<TSignal>;
            if (callback == null) return;
            
            Delegate[] listeners = callback.GetInvocationList();
            
            for (int i = 0; i < listeners.Length; i++)
            {
                try
                {
                    ((Action<TSignal>)listeners[i]).Invoke(signal);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }
        
        public void Clear()
        {
            _listeners.Clear();
        }
        
        private void Unsubscribe<TSignal>(Action<TSignal> listener)
        {
            Type type = typeof(TSignal);
            
            if (!_listeners.TryGetValue(type, out Delegate current))
                return;
            
            Delegate next = Delegate.Remove(current, listener);
            
            if (next == null)
                _listeners.Remove(type);
            else
                _listeners[type] = next;
        }
        
    }
}
