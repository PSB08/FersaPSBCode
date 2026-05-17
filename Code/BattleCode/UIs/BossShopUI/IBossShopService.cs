using System.Collections.Generic;
using YIS.Code.Items;

namespace PSB.Code.BattleCode.UIs.BossShopUI
{
    public interface IBossShopService
    {
        List<UnlockDataSO> LoadItems();
        List<UnlockDataSO> LoadSkills();
        List<DetailDataSO> LoadDetailData();
        bool UnlockItem(UnlockDataSO item);
        bool IsUnlocked(UnlockDataSO item);
    }
}