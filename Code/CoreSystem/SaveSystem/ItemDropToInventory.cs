using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Defines;

namespace PSB.Code.CoreSystem.SaveSystem
{
    public class ItemDropToInventory : MonoBehaviour
    {
        [SerializeField] private InventoryCode targetInventoryCode;

        private void OnEnable()
        {
            Bus<ItemDropped>.OnEvent += OnItemDropped;
        }

        private void OnDisable()
        {
            Bus<ItemDropped>.OnEvent -= OnItemDropped;
        }

        private void OnItemDropped(ItemDropped evt)
        {
            var result = evt.Result;
            if (result == null || result.items == null || result.items.Count == 0)
                return;

            foreach (var d in result.items)
            {
                if (d == null || d.item == null || d.amount <= 0)
                    continue;

                bool isCurrency = IsCurrency(d.item.itemType);

                if (targetInventoryCode == null && !isCurrency)
                {
                    Debug.LogWarning("[ItemDropToInventory] targetInventory가 비어 있습니다.");
                    return;
                }

                if (evt.ApplyMode == LootApplyMode.BattleSession)
                {
                    if (isCurrency)
                    {
                        CurrencyContainer.Add(d.item.itemType, d.amount);
                        Bus<BattleCurrencyAddedEvent>.Raise(
                            new BattleCurrencyAddedEvent(d.item.itemType, d.amount));
                    }
                    else
                    {
                        int addedCount = 0;
                        for (int i = 0; i < d.amount; i++)
                        {
                            bool added = targetInventoryCode.TryAddItem(d.item);
                            if (!added) break;
                            addedCount++;
                        }

                        if (addedCount > 0)
                        {
                            Bus<BattleLootAddedEvent>.Raise(
                                new BattleLootAddedEvent(d.item, addedCount));
                        }
                    }
                }
                else
                {
                    if (isCurrency)
                    {
                        CurrencyContainer.Add(d.item.itemType, d.amount);
                    }
                    else
                    {
                        for (int i = 0; i < d.amount; i++)
                        {
                            bool added = targetInventoryCode.TryAddItem(d.item);
                            if (!added) break;
                        }
                    }
                }
            }

            if (evt.ApplyMode == LootApplyMode.Immediate)
                Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
        }
        
        private static bool IsCurrency(ItemType type)
        {
            return type == ItemType.Coin
                   || type == ItemType.BossCoin
                   || type == ItemType.PP;
        }
        
    }
}