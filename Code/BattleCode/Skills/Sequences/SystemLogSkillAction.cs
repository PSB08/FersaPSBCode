using System.Collections.Generic;
using System.Threading.Tasks;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Skills.Sequences;

namespace PSB.Code.BattleCode.Skills.Sequences
{
    public class SystemLogSkillAction : ISkillAction
    {
        private readonly string _message;
        private readonly SystemLogOwner _owner;
        private readonly Dictionary<string, SystemLogLinkData> _linkDataTable;

        public SystemLogSkillAction(string message, SystemLogOwner owner, 
            Dictionary<string, SystemLogLinkData> linkDataTable = null)
        {
            _message = message;
            _owner = owner;
            _linkDataTable = linkDataTable;
        }

        public async Task ExecuteAsync()
        {
            if (!string.IsNullOrEmpty(_message))
            {
                Bus<SystemLogEvent>.Raise(new SystemLogEvent(_message, _owner, _linkDataTable));
            }

            await Task.CompletedTask;
        }
        
    }
}