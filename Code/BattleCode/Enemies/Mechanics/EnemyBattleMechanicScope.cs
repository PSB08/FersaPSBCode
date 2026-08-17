using System;
using System.Collections.Generic;

namespace PSB.Code.BattleCode.Enemies.Mechanics
{
    public sealed class EnemyBattleMechanicScope : IDisposable
    {
        private readonly Dictionary<string, object> _states = new(StringComparer.Ordinal);
        
        public EnemyMechanicSignalHub Signals { get; } = new EnemyMechanicSignalHub();
        
        public bool TryGetState<T>(string key, out T value)
        {
            value = default;
            
            if (string.IsNullOrEmpty(key))
                return false;
            
            if (!_states.TryGetValue(key, out object state) || state is not T castedState)
                return false;
            
            value = castedState;
            return true;
        }
        
        public T GetState<T>(string key, T defaultValue = default)
        {
            return TryGetState(key, out T value) ? value : defaultValue;
        }
        
        public T GetOrCreateState<T>(string key, Func<T> factory)
        {
            if (TryGetState(key, out T value))
                return value;
            
            value = factory != null ? factory.Invoke() : default;
            SetState(key, value);
            return value;
        }
        
        public void SetState<T>(string key, T value)
        {
            if (!string.IsNullOrEmpty(key))
                _states[key] = value;
        }
        
        public int AddInt(string key, int value)
        {
            int result = GetState(key, 0) + value;
            SetState(key, result);
            return result;
        }
        
        public void RemoveState(string key)
        {
            if (!string.IsNullOrEmpty(key))
                _states.Remove(key);
        }
        
        public void ClearValues()
        {
            _states.Clear();
        }
        
        public void Dispose()
        {
            _states.Clear();
            Signals.Clear();
        }
        
    }
}
