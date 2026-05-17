using PSW.Code.EventBus;
using UnityEngine;

namespace PSB.Code.BattleCode.Events
{
    public struct HealTextUiEvent : IEvent
    {
        public Vector2 TargetPos;
        public float Value;

        public HealTextUiEvent(Vector2 targetPos, float value)
        {
            TargetPos = targetPos;
            Value = value;
        }
        
    }
}