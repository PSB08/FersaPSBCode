using System;
using System.Collections.Generic;
using YIS.Code.Items;

namespace PSB.Code.BattleCode.Items
{
    [Serializable]
    public class DroppedItem
    {
        public ItemDataSO item;
        public int amount;
    }
    
    [Serializable]
    public class DropResultTable
    {
        public List<DroppedItem> items = new List<DroppedItem>();

        public void Add(ItemDataSO item, int amount)
        {
            if (amount <= 0 || item == null)
                return;
            
            var exist = items.Find(x => x.item == item);
            if (exist != null)
            {
                exist.amount += amount;
            }
            else
            {
                items.Add(new DroppedItem
                {
                    item = item,
                    amount = amount
                });
            }
        }
    }
    
}