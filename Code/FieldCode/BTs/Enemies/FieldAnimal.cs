using CIW.Code;
using Code.Scripts.Entities;
using PSB.Code.FieldCode.BTs.Events;
using Unity.Behavior;
using UnityEngine;
using Work.PSB.Code.FieldCode.BTs;

namespace Code.Scripts.Enemies
{
    public class FieldAnimal : Entity
    {
        public BehaviorGraphAgent BtAgent { get; private set; }
        public EntityRenderer entityRenderer;

        protected override void AddComponents()
        {
            base.AddComponents();
            entityRenderer = GetModule<EntityRenderer>();
            BtAgent = GetComponent<BehaviorGraphAgent>();
            Debug.Assert(BtAgent != null, $"{gameObject.name} don't have BehaviorGraphAgent");
        }
        
        public BlackboardVariable<T> GetBlackboardVariable<T>(string key)
        {
            if (BtAgent.GetVariable(key, out BlackboardVariable<T> result))
            {
                return result;
            }
            return default;
        }
        
        private StateChange _stateChannel;

        protected override void Start()
        {
            _stateChannel = GetBlackboardVariable<StateChange>("StateChange").Value;
            entityRenderer.OnDeadEndTrigger += DestroyObject;
        }

        private void OnDestroy()
        {
            entityRenderer.OnDeadEndTrigger -= DestroyObject;
        }
        
        private void DestroyObject()
        {
            Destroy(gameObject);
        }
        
        public void ChangeAttack()
        {
            _stateChannel.SendEventMessage(EnemyState.Attack);
        }
        
    }
}