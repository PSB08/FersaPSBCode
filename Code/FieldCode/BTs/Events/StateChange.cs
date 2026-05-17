using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Work.PSB.Code.FieldCode.BTs;

namespace PSB.Code.FieldCode.BTs.Events
{
    [CreateAssetMenu(menuName = "Behavior/Event Channels/ChangeNewValue")]
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "ChangeNewValue", message: "state change to [NewValue]", category: "Events",
        id: "d445d55bb0cb03f747f79b86de33cea3")]
    public partial class StateChange : EventChannel<EnemyState>
    {
        public delegate void StateChangeEventHandler(EnemyState newValue);
        public new event StateChangeEventHandler Event; 

        public new void SendEventMessage(EnemyState newValue)
        {
            Event?.Invoke(newValue);
        }

        public override void SendEventMessage(BlackboardVariable[] messageData)
        {
            BlackboardVariable<EnemyState> newValueBlackboardVariable = messageData[0] as BlackboardVariable<EnemyState>;
            var newValue = newValueBlackboardVariable != null ? newValueBlackboardVariable.Value : default(EnemyState);

            Event?.Invoke(newValue);
        }

        public override Delegate CreateEventHandler(BlackboardVariable[] vars, global::System.Action callback)
        {
            StateChangeEventHandler del = (newValue) =>
            {
                BlackboardVariable<EnemyState> var0 = vars[0] as BlackboardVariable<EnemyState>;
                if(var0 != null)
                    var0.Value = newValue;

                callback();
            };
            return del;
        }

        public override void RegisterListener(Delegate del)
        {
            Event += del as StateChangeEventHandler;
        }

        public override void UnregisterListener(Delegate del)
        {
            Event -= del as StateChangeEventHandler;
        }
        
    }
}

