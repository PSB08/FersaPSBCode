using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.BattleCode.Events
{
    public enum BuffUiOp
    {
        Applied,
        Updated,
        Removed
    }

    public struct BuffUiEvent : IEvent
    {
        public readonly ModuleOwner Target;
        public readonly BuffVisualSO BuffVisualData;
        public readonly BuffUiOp Op;

        public readonly float Value;
        public readonly int Duration;

        public BuffUiEvent(ModuleOwner target, BuffVisualSO buffVisualData, BuffUiOp op, float value, int duration)
        {
            Target = target;
            BuffVisualData = buffVisualData;
            Op = op;
            Value = value;
            Duration = duration;
        }
    }
    
}