using System.Collections.Generic;
using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Defines;

namespace PSB.Code.BattleCode.Items
{
    public class ItemDropDebug : MonoBehaviour
    {
        private readonly Dictionary<string, int> _itemSessionTotal = new();
        private readonly Dictionary<ItemType, int> _currencySessionTotal = new();

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
            {
                Debug.Log("[DropDebug] 이번 드랍: 아무 것도 없음");
                return;
            }

            Dictionary<string, int> thisItemDrop = new();
            Dictionary<ItemType, int> thisCurrencyDrop = new();

            foreach (var d in result.items)
            {
                if (d == null || d.item == null || d.amount <= 0)
                    continue;
                
                if (IsCurrency(d.item.itemType))
                {
                    Add(thisCurrencyDrop, d.item.itemType, d.amount);
                    Add(_currencySessionTotal, d.item.itemType, d.amount);
                }
                else
                {
                    string key = d.item.name;
                    Add(thisItemDrop, key, d.amount);
                    Add(_itemSessionTotal, key, d.amount);
                }
            }

            DebugThisDrop(thisItemDrop, thisCurrencyDrop);
            DebugSessionTotal();
        }
        
        private void DebugThisDrop(
            Dictionary<string, int> items,
            Dictionary<ItemType, int> currencies)
        {
            Debug.Log("<color=cyan>[DropDebug] 이번 드랍 결과</color>");

            foreach (var c in currencies)
                Debug.Log($"  재화: {c.Key} x{c.Value}");

            foreach (var i in items)
                Debug.Log($"  아이템: {i.Key} x{i.Value}");
        }

        private void DebugSessionTotal()
        {
            foreach (var c in _currencySessionTotal)
                Debug.Log($"  재화 누적: {c.Key} x{c.Value}");

            foreach (var i in _itemSessionTotal)
                Debug.Log($"  아이템 누적: {i.Key} x{i.Value}");
        }

        private static bool IsCurrency(ItemType type)
        {
            return type == ItemType.Coin
                || type == ItemType.BossCoin
                || type == ItemType.PP;
        }

        private static void Add<TKey>(Dictionary<TKey, int> map, TKey key, int amount)
        {
            if (map.TryGetValue(key, out int v))
                map[key] = v + amount;
            else
                map[key] = amount;
        }
        
    }
}
