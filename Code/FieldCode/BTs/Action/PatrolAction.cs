using System;
using CIW.Code;
using Code.Scripts.Enemies;
using Code.Scripts.Enemies.Astar;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace PSB.Code.FieldCode.BTs.Action
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Patrol", story: "[Self] Patrol with [Points]", category: "Action", id: "8fd21ad3b9d447b859fc09f78a40fff5")]
    public partial class PatrolAction : Unity.Behavior.Action
    {
        [SerializeReference] public BlackboardVariable<Entity> Self;
        [SerializeReference] public BlackboardVariable<WayPoints> Points;

        private int _currentPointIdx;
        private PathMovement _navMovement;

        protected override Status OnStart()
        {
            Initialize();
            if (Points.Value != null && Points.Value.gameObject.activeInHierarchy)
            {
                _navMovement.SetDestination(Points.Value[_currentPointIdx]);
                return Status.Running;
            }
            else
            {
                return Status.Failure;
            }
        }

        private void Initialize()
        {
            if (_navMovement == null)
                _navMovement = Self.Value.GetModule<PathMovement>();
        }

        protected override Status OnUpdate()
        {
            if (_navMovement.IsArrived)
                return Status.Success;
            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (Points.Value != null && Points.Value.gameObject.activeInHierarchy)
                _currentPointIdx = (_currentPointIdx + 1) % Points.Value.Length;
        }
    
    }
}

