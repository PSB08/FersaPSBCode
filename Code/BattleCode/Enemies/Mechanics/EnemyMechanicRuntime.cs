using System;
using PSB.Code.BattleCode.Enemies.Mechanics.ActionGates;
using PSB.Code.BattleCode.Enemies.Mechanics.Intents;
using PSB.Code.BattleCode.Enemies.Mechanics.Phases;

namespace PSB.Code.BattleCode.Enemies.Mechanics
{
    public abstract class EnemyMechanicRuntime
    {
        private readonly EnemyMechanicSubscriptionBag _subscriptions = new();
        
        protected EnemyMechanicContext Context { get; }
        protected EnemyMechanicSO Definition { get; }
        
        public bool IsAttached { get; private set; }
        
        protected EnemyMechanicRuntime(EnemyMechanicContext context, EnemyMechanicSO definition)
        {
            Context = context;
            Definition = definition;
        }
        
        public void Attach()
        {
            if (IsAttached) return;
            
            IsAttached = true;
            
            try
            {
                RegisterExtensions();
                OnAttach();
            }
            catch
            {
                Detach();
                throw;
            }
        }
        
        public void Detach()
        {
            if (!IsAttached) return;
            
            try
            {
                OnDetach();
            }
            finally
            {
                IsAttached = false;
                _subscriptions.Dispose();
            }
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
        
        protected void Publish<TSignal>(TSignal signal)
        {
            Context.Host?.Publish(signal);
        }
        
        private void RegisterExtensions()
        {
            if (this is IEnemyIntentContributor intentContributor)
                _subscriptions.Add(Context.Intents.Register(intentContributor));
            
            if (this is IEnemyPhaseTransitionGuard phaseGuard)
                _subscriptions.Add(Context.PhaseTransitions.Register(phaseGuard));
            
            if (this is IEnemyActionGate actionGate)
                _subscriptions.Add(Context.ActionGates.Register(actionGate));
        }
        
    }
}
