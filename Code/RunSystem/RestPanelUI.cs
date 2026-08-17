using System;
using PSB.Code.BattleCode.Events;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.UI;

namespace Work.PSB.Code.RunSystem
{
    public class RestPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject skillUpgradePanel;
        
        [SerializeField] private Button healBtn;
        [SerializeField] private Button upgradeBtn;
        
        private float _healPercent;
        private Action _onComplete;
        private bool _used;
        
        private void Awake()
        {
            if (healBtn != null)
            {
                healBtn.onClick.RemoveListener(OnClickHeal);
                healBtn.onClick.AddListener(OnClickHeal);
            }
            
            if (upgradeBtn != null)
            {
                upgradeBtn.onClick.RemoveListener(OnClickUpgradeSkill);
                upgradeBtn.onClick.AddListener(OnClickUpgradeSkill);
            }
            
            CloseImmediate();
        }
        
        private void OnDestroy()
        {
            if (healBtn != null)
                healBtn.onClick.RemoveListener(OnClickHeal);
            
            if (upgradeBtn != null)
                upgradeBtn.onClick.RemoveListener(OnClickUpgradeSkill);
        }
        
        public void Open(float healPercent, Action onComplete)
        {
            _healPercent = healPercent;
            _onComplete = onComplete;
            _used = false;
            gameObject.SetActive(true);
            panelRoot.SetActive(true);
            
            if (skillUpgradePanel != null)
                skillUpgradePanel.SetActive(false);
        }
        
        public void OnClickHeal()
        {
            if (_used)
                return;
            
            _used = true;
            Bus<HealRequest>.Raise(new HealRequest(_healPercent, HealMode.MaxPercent));
            Complete();
        }
        
        public void OnClickUpgradeSkill()
        {
            if (_used)
                return;
            
            _used = true;
            
            if (skillUpgradePanel != null)
                skillUpgradePanel.SetActive(true);
        }
        
        public void NotifySkillUpgradeFinished()
        {
            if (_used)
                Complete();
        }
        
        private void Complete()
        {
            CloseImmediate();
            _onComplete?.Invoke();
            _onComplete = null;
        }
        
        private void CloseImmediate()
        {
            panelRoot.SetActive(false);
            
            if (skillUpgradePanel != null)
                skillUpgradePanel.SetActive(false);
        }
        
    }
}
