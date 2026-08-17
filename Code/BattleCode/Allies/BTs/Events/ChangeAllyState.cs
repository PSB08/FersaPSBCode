using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace PSB.Code.BattleCode.Allies.BTs.Events
{
    [CreateAssetMenu(menuName = "Behavior/Event Channels/ChangeAllyState")]
    [Serializable, GeneratePropertyBag]
    [EventChannelDescription(name: "ChangeAllyState", message: "ally state change to [NewState]", category: "Events")]
    public sealed partial class ChangeAllyState : EventChannel<BattleAllyState>
    {
        public delegate void StateChangeEventHandler(BattleAllyState newValue);
        public new event StateChangeEventHandler Event;

        public new void SendEventMessage(BattleAllyState newValue) => Event?.Invoke(newValue);

        public override void SendEventMessage(BlackboardVariable[] messageData)
        {
            var newValueVar = messageData[0] as BlackboardVariable<BattleAllyState>;
            var newValue = newValueVar != null ? newValueVar.Value : default;
            Event?.Invoke(newValue);
        }

        public override Delegate CreateEventHandler(BlackboardVariable[] vars, global::System.Action callback)
        {
            StateChangeEventHandler del = newValue =>
            {
                BlackboardVariable<BattleAllyState> var0 = vars[0] as BlackboardVariable<BattleAllyState>;
                if (var0 != null) var0.Value = newValue;
                callback();
            };
            return del;
        }

        public override void RegisterListener(Delegate del) => Event += del as StateChangeEventHandler;
        public override void UnregisterListener(Delegate del) => Event -= del as StateChangeEventHandler;
    }
}