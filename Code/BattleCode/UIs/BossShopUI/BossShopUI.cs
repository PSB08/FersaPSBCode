using PSB.Code.CoreSystem.Events;
using PSB_Lib.Dependencies;
using PSW.Code.BaseSystem;
using PSW.Code.EventBus;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Work.PSB.Code.CoreSystem.Sounds;
using YIS.Code.UI;
using YIS.Code.Defines;
using YIS.Code.Events;
using YIS.Code.Items;

namespace PSB.Code.BattleCode.UIs.BossShopUI
{
    public class BossShopUI : BaseOnOffAnimationUI, IBaseSystemUI
    {
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Inject] private IBossShopService _service;

        [SerializeField] private Transform itemRoot;
        [SerializeField] private Transform skillRoot;
        [SerializeField] private Transform detailRoot;
        
        [SerializeField] private SoundSO clickSound;
        [SerializeField] private SoundSO errorSound;
        [SerializeField] private SoundSO buySound;

        private List<UnlockItemUI> _shopItems;
        private List<UnlockItemUI> _shopSkills;
        
        private Dictionary<DetailUIType, DetailBtnUI> _detailDatas;

        public override void Awake()
        {
            base.Awake();
            _detailDatas = new Dictionary<DetailUIType, DetailBtnUI>();
            _shopItems = itemRoot.GetComponentsInChildren<UnlockItemUI>().ToList();
            _shopSkills = skillRoot.GetComponentsInChildren<UnlockItemUI>().ToList();
            _detailDatas = detailRoot.GetComponentsInChildren<DetailBtnUI>().
                ToDictionary(item => item.DetailType, item => item);
            
            RegisterEvent();
        }

        private void Start()
        {
            RegisterBtnEvent();
            VisualLoad();

            SetActive((false));
        }

        private void RegisterEvent()
        {
            Bus<BossShopRefreshEvent>.OnEvent += HandleRefresh;
        }

        public void SetActive(bool isActive)
        {
            canvasGroup.alpha = isActive ? 1 : 0;
            canvasGroup.interactable = isActive;
            canvasGroup.blocksRaycasts = isActive;
        }

        private void HandleRefresh(BossShopRefreshEvent evt)
        {
            ResetItems();
            VisualLoad();
            RegisterBtnEvent();
        }

        private void RegisterBtnEvent()
        {
            ResetEvent();

            List<UnlockDataSO> itemList = _service.LoadItems();
            List<UnlockDataSO> skillList = _service.LoadSkills();
            List<DetailDataSO> detailList = _service.LoadDetailData();

            for (int i = 0; i < itemList.Count && i < _shopItems.Count; i++)
            {
                UnlockItemUI ui = _shopItems[i];
                Button btn = ui.Btn;

                btn.onClick.AddListener(() =>
                {
                    UnlockItem(ui.ItemDataSO);
                });
            }

            for (int i = 0; i < skillList.Count && i < _shopSkills.Count; i++)
            {
                UnlockItemUI ui = _shopSkills[i];
                Button btn = ui.Btn;

                btn.onClick.AddListener(() =>
                {
                    UnlockItem(ui.ItemDataSO);
                });
            }
            for (int i = 0; i < detailList.Count; i++)
            {
                var detail = detailList[i];

                DetailBtnUI ui = _detailDatas[detail.detailType];
                Button btn = _detailDatas[detail.detailType].Btn;
                btn.onClick.AddListener(() =>
                {
                    UseDetail(ui);
                });
            }
        }

        private void ResetEvent()
        {
            foreach (var ui in _shopItems)
                ui.Btn.onClick.RemoveAllListeners();

            foreach (var ui in _shopSkills)
                ui.Btn.onClick.RemoveAllListeners();
            foreach (DetailBtnUI ui in _detailDatas.Values) ui.Btn.onClick.RemoveAllListeners();
        }

        private void VisualLoad()
        {
            List<UnlockDataSO> itemList = _service.LoadItems();
            List<UnlockDataSO> skillList = _service.LoadSkills();
            List<DetailDataSO> detailList = _service.LoadDetailData();

            for (int i = 0; i < _shopItems.Count; i++)
            {
                if (i < itemList.Count)
                {
                    _shopItems[i].Initialize(itemList[i]);
                    _shopItems[i].gameObject.SetActive(true);
                }
                else
                {
                    _shopItems[i].gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < _shopSkills.Count; i++)
            {
                if (i < skillList.Count)
                {
                    _shopSkills[i].Initialize(skillList[i]);
                    _shopSkills[i].gameObject.SetActive(true);
                }
                else
                {
                    _shopSkills[i].gameObject.SetActive(false);
                }
            }
            
            for (int i = 0; i < detailList.Count; i++)
            {
                var detail = detailList[i];

                DetailBtnUI ui = _detailDatas[detail.detailType];
                ui.InitData(detail);
            }
        }

        private void ResetItems()
        {
            foreach (var ui in _shopItems)
                ui.ResetItem();

            foreach (var ui in _shopSkills)
                ui.ResetItem();
        }
        
        private void UseDetail(DetailBtnUI detail)
        {
            switch (detail.DetailType)
            {
                case DetailUIType.Quit:
                    Quit();
                    break;
            }
        }
        
        private void Quit()
        {
            PlaySfx(clickSound);
            PopUp();
        }

        private bool UnlockItem(UnlockDataSO item)
        {
            if (!_service.UnlockItem(item))
            {
                PlaySfx(errorSound);
                Debug.Log("<color=red>해금 실패!</color>");
                return false;
            }

            PlaySfx(buySound);
            Debug.Log("<color=green>해금 성공!</color>");
            return true;
        }

        private void PlaySfx(SoundSO soundSO)
        {
            if (soundSO == null || soundSO.clip == null)
                return;

            Bus<PlaySFXEvent>.Raise(SoundEvents.PlaySFXEvent.Initialize(transform.position, soundSO));
        }

        private void OnDisable()
        {
            Bus<DescUIActiveEvent>.Raise(new DescUIActiveEvent(false));
        }

        private void OnDestroy()
        {
            Bus<BossShopRefreshEvent>.OnEvent -= HandleRefresh;

            ResetEvent();
        }

        public void DataInit()
        {
        }

        public void DataDestroy()
        {
            Bus<DescUIActiveEvent>.Raise(new DescUIActiveEvent(false));
        }
    }
}