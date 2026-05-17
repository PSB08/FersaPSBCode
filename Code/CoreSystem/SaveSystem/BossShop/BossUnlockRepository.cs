using System.Collections.Generic;
using System.Linq;
using PSB_Lib.Dependencies;
using UnityEngine;

namespace PSB.Code.CoreSystem.SaveSystem.BossShop
{
    [Provide]
    public class BossUnlockRepository : MonoBehaviour, ISaveable, IDependencyProvider
    {
        [field: SerializeField] public SaveId SaveId { get; private set; }
        
        private HashSet<int> _unlockedIds = new();
        
        public HashSet<int> UnlockedIds => _unlockedIds;
        
        [Inject] private ISaveStore _store;
        

        public bool IsUnlocked(int id)
        {
            return _unlockedIds.Contains(id);
        }

        public bool Unlock(int id)
        {
            return _unlockedIds.Add(id);
        }

        private void ClearAll()
        {
            _unlockedIds.Clear();
            _store.DeleteById(SaveId);
        }

        public string GetSaveData()
        {
            BossUnlockSaveData data = new BossUnlockSaveData
            {
                unlockedShopItems = _unlockedIds.ToList()
            };

            return JsonUtility.ToJson(data);
        }

        public void RestoreSaveData(string saveData)
        {
            _unlockedIds.Clear();

            if (string.IsNullOrWhiteSpace(saveData))
                return;

            BossUnlockSaveData data = JsonUtility.FromJson<BossUnlockSaveData>(saveData);
            if (data.unlockedShopItems == null) return;

            foreach (int id in data.unlockedShopItems)
            {
                _unlockedIds.Add(id);
            }
        }
        
    }
}