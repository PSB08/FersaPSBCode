using System;

namespace PSB.Code.BattleCode.Enemies.Mechanics
{
    public sealed class EnemyMechanicSubscription : IDisposable
    {
        private Action _dispose;
        
        public EnemyMechanicSubscription(Action dispose)
        {
            _dispose = dispose;
        }
        
        public void Dispose()
        {
            Action dispose = _dispose;
            _dispose = null;
            dispose?.Invoke();
        }
        
    }
}
