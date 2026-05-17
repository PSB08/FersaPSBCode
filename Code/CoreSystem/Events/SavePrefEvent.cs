using System;
using PSW.Code.EventBus;

namespace PSB.Code.CoreSystem.Events
{
    public class LoadPrefEvent : IEvent { }
    
    public class SavePrefEvent : IEvent
    {
        public Action Callback;

        public SavePrefEvent(Action callback)
        {
            Callback = callback;
        }
        
    }
}