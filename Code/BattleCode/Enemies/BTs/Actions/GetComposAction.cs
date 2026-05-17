using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using YIS.Code.Modules;
using Action = Unity.Behavior.Action;

namespace PSB.Code.BattleCode.Enemies.BTs.Actions
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "GetCompos", story: "get components from [Self]", category: "Action", id: "31e6fc54931566c09886165bdae567ca")]
    public partial class GetComposAction : Action
    {
        [SerializeReference] public BlackboardVariable<BattleEnemy> Self;

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

