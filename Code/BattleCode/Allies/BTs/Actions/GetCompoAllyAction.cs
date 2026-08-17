using PSB.Code.BattleCode.Allies;
using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using YIS.Code.Modules;
using Action = Unity.Behavior.Action;

namespace Work.PSB.Code.BattleCode.Allies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "GetCompoAllyAction", story: "get compos ally [Self]", category: "Action", id: "d79c8a155932e61fb92eb9642d703746")]
    public partial class GetCompoAllyAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleAlly> Self;

        protected override Status OnStart()
        {
            List<BlackboardVariable> variableList = Self.Value.BtAgent.BlackboardReference.Blackboard.Variables;

            foreach (BlackboardVariable variable in variableList)
            {
                if (typeof(IModule).IsAssignableFrom(variable.Type) == false) continue;

                IModule targetComponent = Self.Value.GetModule(variable.Type);
                Debug.Assert(targetComponent != null, $"{variable.Name} is not exist on {Self.Value.gameObject.name}");
                variable.ObjectValue = targetComponent;
            }
            return Status.Success;
        }
        
    }
}

