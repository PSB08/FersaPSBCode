using System;
using UnityEngine;
using YIS.Code.Defines;
using YIS.Code.Items;

namespace PSB.Code.BattleCode.UIs.BossShopUI
{
    [CreateAssetMenu(fileName = "New UnlockData", menuName = "SO/Item/UnlockItemData", order = 0)]
    public class UnlockDataSO : ScriptableObject
    {
        [Tooltip("ID 규칙 : 비워두면 shopItemData를 따라갑니다")] public int id;
        public ItemType currencyType = ItemType.Coin;
        public int itemPrice;
        public ShopItemDataSO shopItemData;

        private void OnValidate()
        {
            id = shopItemData.id;
        }
        
    }
}