using PSB.Code.CoreSystem.Events;
using PSB.Code.CoreSystem.SaveSystem;
using PSW.Code.EventBus;
using UnityEngine;

namespace PSB.Code.BattleCode.BattleSystems
{
    public class BattleLootRollbackSystem : MonoBehaviour
    {
        [SerializeField] private InventoryCode inventory;

        private void OnEnable()
        {
            Bus<RollbackBattleLootEvent>.OnEvent += OnRollback;
        }

        private void OnDisable()
        {
            Bus<RollbackBattleLootEvent>.OnEvent -= OnRollback;
        }

        private void OnRollback(RollbackBattleLootEvent evt)
        {
            BattleLootSession.Instance?.Rollback(inventory);
        }
        
    }
}