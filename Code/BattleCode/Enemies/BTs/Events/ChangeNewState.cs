using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.BTs.Events
{
    [CreateAssetMenu(menuName = "Behavior/Event Channels/ChangeNewState")]
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "ChangeNewState", message: "state change to [NewState]", category: "Events",
        id: "ea336a568857bc10647dd723ba917768")]
    public sealed partial class ChangeNewState : EventChannel<BattleEnemyState>
    {
        public delegate void StateChangeEventHandler(BattleEnemyState newValue);
        public new event StateChangeEventHandler Event; 

        public new void SendEventMessage(BattleEnemyState newValue)
        {
            Event?.Invoke(newValue);
        }

        public override void SendEventMessage(BlackboardVariable[] messageData)
        {
            BlackboardVariable<BattleEnemyState> newValueBlackboardVariable = messageData[0] as BlackboardVariable<BattleEnemyState>;
            var newValue = newValueBlackboardVariable != null ? newValueBlackboardVariable.Value : default(BattleEnemyState);

            Event?.Invoke(newValue);
        }

        public override Delegate CreateEventHandler(BlackboardVariable[] vars, global::System.Action callback)
        {
            StateChangeEventHandler del = (newValue) =>
            {
                BlackboardVariable<BattleEnemyState> var0 = vars[0] as BlackboardVariable<BattleEnemyState>;
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

