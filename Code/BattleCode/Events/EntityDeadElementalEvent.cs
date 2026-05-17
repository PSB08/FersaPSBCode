using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Defines;

namespace PSB.Code.BattleCode.Events
{
    public struct EntityDeadElementalEvent : IEvent
    {
        public GameObject Target;
        public Elemental ElementalType;
        
        public EntityDeadElementalEvent(GameObject target, Elemental type)
        {
            Target = target;
            ElementalType = type;
        }
        
    }
}