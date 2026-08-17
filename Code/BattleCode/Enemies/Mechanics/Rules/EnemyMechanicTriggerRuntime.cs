using System;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules
{
    public abstract class EnemyMechanicTriggerRuntime
    {
        private readonly EnemyMechanicSubscriptionBag _subscriptions =
            new EnemyMechanicSubscriptionBag();
        
        private readonly Action<object> _execute;
        private bool _attached;
        
        protected EnemyMechanicContext Context { get; }
        
        protected EnemyMechanicTriggerRuntime(EnemyMechanicContext context, Action<object> execute)
        {
            Context = context;
            _execute = execute;
        }
        
        public void Attach()
        {
            if (_attached) return;
            
            _attached = true;
            OnAttach();
        }
        
        public void Detach()
        {
            if (!_attached) return;
            
            _attached = false;
            OnDetach();
            _subscriptions.Dispose();
        }
        
        protected virtual void OnAttach()
        {
        }
        
        protected virtual void OnDetach()
        {
        }
        
        protected void Listen<TSignal>(Action<TSignal> listener)
        {
            _subscriptions.Add(Context.Signals.Subscribe(listener));
        }
        
        protected void ListenBattle<TSignal>(Action<TSignal> listener)
        {
            _subscriptions.Add(Context.BattleScope?.Signals.Subscribe(listener));
        }
        
        protected void Fire(object signal)
        {
            _execute?.Invoke(signal);
        }
        
    }
}
