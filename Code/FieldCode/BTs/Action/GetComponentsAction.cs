using System;
using System.Collections.Generic;
using Code.Scripts.Enemies;
using PSB.Code.BattleCode.Entities;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using YIS.Code.Modules;

namespace PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "GetComponents", story: "get components from [Self]", category: "Action", id: "45abec7f5bd9df71e3e4b6c25a6803d4")]
    public partial class GetComponentsAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<FieldEnemy> Self;

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

