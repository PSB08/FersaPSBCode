using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YIS.Code.Items;

namespace PSB.Code.CoreSystem.SaveSystem
{
    [CreateAssetMenu(fileName = "Item database", menuName = "SO/DB/Item db", order = 0)]
    public class ItemDatabase : ScriptableObject
    {
        [field: SerializeField] public List<ItemDataSO> Items { get; private set; }

        public ItemDataSO GetItemData(int id)
            => Items.FirstOrDefault(item => item.itemId == id);

        private void OnValidate()
        {
            var hashSet = Items.ToHashSet();
            if (hashSet.Count != Items.Count)
            {
                Debug.LogWarning("중복된 Id가 있습니다. 선생님");
            }
        }
        
    }
}