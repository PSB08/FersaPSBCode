using UnityEngine;
using YIS.Code.Items;

namespace PSB.Code.BattleCode.UIs.BossShopUI
{
    [CreateAssetMenu(fileName = "BossShopDataList", menuName = "SO/Item/BossShopList", order = 10)]
    public class BossItemListSO : ScriptableObject
    {
        public UnlockDataSO[] shopItemDataList;
    }
}