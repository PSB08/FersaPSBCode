using CIW.Code;
using Code.Scripts.Entities;
using Unity.Behavior;
using UnityEngine;

namespace Code.Scripts.Enemies
{
    public class FieldEnemy : Entity
    {
        public BehaviorGraphAgent BtAgent { get; private set; }
        public EntityRenderer EntityRenderer;

        protected override void AddComponents()
        {
            base.AddComponents();
            
            EntityRenderer = GetModule<EntityRenderer>();
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

        
    }
}