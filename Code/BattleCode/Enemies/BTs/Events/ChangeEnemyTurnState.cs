using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace PSB.Code.BattleCode.Enemies.BTs.Events
{
    [CreateAssetMenu(menuName = "Behavior/Event Channels/ChangeEnemyTurnState")]
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "ChangeEnemyTurnState", message: "enemy turn state change to [NewState]", category: "Events", id: "f37a6b2c9f4e4d3f9f1f6a1e5b2a8c10")]
    public sealed partial class ChangeEnemyTurnState : EventChannel<BattleEnemyTurnState>
    {
        public delegate void StateChangeEventHandler(BattleEnemyTurnState newValue);
        public new event StateChangeEventHandler Event;

        public new void SendEventMessage(BattleEnemyTurnState newValue)
        {
            Event?.Invoke(newValue);
        }

        public override void SendEventMessage(BlackboardVariable[] messageData)
        {
            BlackboardVariable<BattleEnemyTurnState> newValueBlackboardVariable =
                messageData[0] as BlackboardVariable<BattleEnemyTurnState>;
            var newValue = newValueBlackboardVariable != null ? newValueBlackboardVariable.Value
                : default(BattleEnemyTurnState);

            Event?.Invoke(newValue);
        }

        public override Delegate CreateEventHandler(BlackboardVariable[] vars, global::System.Action callback)
        {
            StateChangeEventHandler del = newValue =>
            {
                BlackboardVariable<BattleEnemyTurnState> var0 = vars[0] as BlackboardVariable<BattleEnemyTurnState>;
                if (var0 != null)
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
