using Code.Scripts.Entities;
using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.Players;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Work.PSB.Code.CoreSystem.UpgradeSystem
{
    public class UpgradeInputBinder : MonoBehaviour
    {
        [SerializeField] private EntityStat playerStat;
        [SerializeField] private UpgradePanelUI upgradeUI;
        
        private UpgradeService _service;

        private void OnEnable()
        {
            _service = new UpgradeService(playerStat);
            //input.OnUpgradePressed += OnUpgradePressed;

            Bus<VillageResetEvent>.OnEvent += HandleVillageReset;
        }

        private void OnDisable()
        {
            //input.OnUpgradePressed -= OnUpgradePressed;
            
            Bus<VillageResetEvent>.OnEvent -= HandleVillageReset;
        }

        private void HandleVillageReset(VillageResetEvent evt)
        {
            _service.ResetAllUpgrades(upgradeUI.GetAllDefs());
            upgradeUI.RefreshAll();
            Debug.Log("<color=purple>마을 초기화 - 업그레이드 정보</color>");
        }

        #if UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                Debug.Log("<color=purple>[Upgrade]초기화</color>");

                _service.ResetAllUpgrades(upgradeUI.GetAllDefs());
                upgradeUI.RefreshAll();
            }
        }
        #endif

        /*private void OnUpgradePressed()
        {
            upgradeUI.Toggle();
        }
        */
    }
}