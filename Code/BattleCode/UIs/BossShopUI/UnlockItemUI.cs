using DG.Tweening;
using PSW.Code.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YIS.Code.Defines;
using YIS.Code.Events;

namespace PSB.Code.BattleCode.UIs.BossShopUI
{
    public class UnlockItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
         [field: SerializeField] public UnlockDataSO ItemDataSO { get; private set; }

        [field: SerializeField] public Button Btn { get; private set; }
        
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private TextMeshProUGUI itemPriceText;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Image currencyIcon;
        
        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Sprite bossCoinIcon;

        [SerializeField] private Material outLineMap;
        [SerializeField] private string setValue = "_BloomPower";
        [SerializeField] private float popTime = 0.25f;
        [SerializeField] private float popUpValue = 10;

        private bool _isSpend = false;
        
        public void Initialize(UnlockDataSO itemDataSO)
        {
            ItemDataSO = itemDataSO;

            SetData(itemDataSO);
            gameObject.SetActive(!_isSpend);
            outLineMap.SetFloat(setValue, 0);
            Btn.onClick.AddListener(() => Btn.image.material = null);
        }

        private void SetData(UnlockDataSO itemDataSO)
        {
            ItemDataSO = itemDataSO;
            switch (itemDataSO.shopItemData.shopItemType)
            {
                case ShopItemType.Item:
                    itemIcon.sprite = itemDataSO.shopItemData.itemData.visualData.icon;
                    itemNameText.SetText(itemDataSO.shopItemData.itemData.visualData.uiName);
                    itemPriceText.SetText(itemDataSO.itemPrice.ToString());
                    break;
                case ShopItemType.Skill:
                    itemIcon.sprite = itemDataSO.shopItemData.skillData.visualData.icon;
                    itemNameText.SetText(itemDataSO.shopItemData.skillData.visualData.uiName);
                    itemPriceText.SetText(itemDataSO.itemPrice.ToString());
                    break;
            }
            SetCurrencyIcon(itemDataSO.currencyType);
        }
        
        private void SetCurrencyIcon(ItemType currencyType)
        {
            if (currencyIcon == null) return;

            switch (currencyType)
            {
                case ItemType.Coin:
                    currencyIcon.sprite = coinIcon;
                    currencyIcon.gameObject.SetActive(coinIcon != null);
                    break;

                case ItemType.BossCoin:
                    currencyIcon.sprite = bossCoinIcon;
                    currencyIcon.gameObject.SetActive(bossCoinIcon != null);
                    break;

                default:
                    currencyIcon.sprite = null;
                    currencyIcon.gameObject.SetActive(false);
                    break;
            }
        }


        public void SpendItem()
        {
            _isSpend = true;
            gameObject.SetActive(false);
        }

        public void ResetItem()
        {
            _isSpend = false;
            outLineMap.SetFloat(setValue, 0);
            gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            outLineMap.SetFloat(setValue, 0);
        }   

        public void OnPointerEnter(PointerEventData eventData)
        {
            outLineMap.DOKill();
            Btn.image.material = outLineMap;
            outLineMap.DOFloat(popUpValue, setValue, popTime);

            Bus<DescUIActiveEvent>.Raise(new DescUIActiveEvent(true));
            switch (ItemDataSO.shopItemData.shopItemType)
            {
                case ShopItemType.Item:
                    Bus<DescUIEvent>.Raise(new DescUIEvent(ItemDataSO.shopItemData.itemData.visualData));
                    break;
                case ShopItemType.Skill:
                    Bus<DescUIEvent>.Raise(new DescUIEvent(ItemDataSO.shopItemData.skillData.visualData));
                    break;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            outLineMap.DOFloat(0, setValue, popTime)
                .OnComplete(() => Btn.image.material = null)
                .OnKill(() => Btn.image.material = null);
            Bus<DescUIActiveEvent>.Raise(new DescUIActiveEvent(false));
        }
        
    }
}