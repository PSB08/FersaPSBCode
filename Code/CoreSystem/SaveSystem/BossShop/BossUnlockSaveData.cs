using System;
using System.Collections.Generic;

namespace PSB.Code.CoreSystem.SaveSystem.BossShop
{
    [Serializable]
    public class BossUnlockSaveData
    {
        public List<int> unlockedShopItems = new List<int>();
    }
}