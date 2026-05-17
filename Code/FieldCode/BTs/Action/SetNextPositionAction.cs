using System;
using Code.Scripts.Enemies;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "SetNextPosition", story: "[Self] set [NextPosition] from [Target]", category: "Action", id: "b530970b728e5cc08d06b034f6d19863")]
    public partial class SetNextPositionAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<FieldEnemy> Self;
        [SerializeReference] public BlackboardVariable<Vector3> NextPosition;
        [SerializeReference] public BlackboardVariable<Transform> Target;

        protected override Status OnStart()
        {
            NextPosition.Value = GetAttackPosition();
            return Status.Running;
        }

        private Vector3 GetAttackPosition()
        {
            Vector3 targetPos = Target.Value.position;
            Vector3 myPos = Self.Value.transform.position;
            
            float xDirection = Mathf.Sign(targetPos.x - myPos.x);
            targetPos.x -= xDirection;
            
            return targetPos;
        }
        
    }
}

