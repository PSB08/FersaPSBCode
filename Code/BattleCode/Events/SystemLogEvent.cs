using System.Collections.Generic;
using PSW.Code.EventBus;

namespace PSB.Code.BattleCode.Events
{
    public enum SystemLogOwner
    {
        Player,
        Enemy,
        Neutral
    }
    
    public struct SystemLogLinkData
    {
        public string Title;
        public string Description;

        public SystemLogLinkData(string title, string description)
        {
            Title = title;
            Description = description;
        }
    }
    
    public struct SystemLogEvent : IEvent
    {
        public string Message;
        public SystemLogOwner Owner;
        public Dictionary<string, SystemLogLinkData> LinkDataTable;

        public SystemLogEvent(string message, SystemLogOwner owner, 
            Dictionary<string, SystemLogLinkData> linkDataTable = null)
        {
            Message = message;
            Owner = owner;
            LinkDataTable = linkDataTable;
        }
        
    }
}