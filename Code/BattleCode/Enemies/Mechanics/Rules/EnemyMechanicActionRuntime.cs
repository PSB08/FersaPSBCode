using System;
using System.Collections.Generic;
using CIW.Code;

namespace PSB.Code.BattleCode.Enemies.Mechanics.Rules
{
    public abstract class EnemyMechanicActionRuntime
    {
        private readonly EnemyMechanicSubscriptionBag _subscriptions =
            new EnemyMechanicSubscriptionBag();
        
        private bool _attached;
        
        protected EnemyMechanicContext Context { get; }
        
        protected EnemyMechanicActionRuntime(EnemyMechanicContext context)
        {
            Context = context;
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
        
        public abstract void Execute(EnemyMechanicExecutionContext executionContext,
            IReadOnlyList<Entity> targets);
        
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
        
    }
}
