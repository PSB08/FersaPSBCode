using System.Collections.Generic;
using System.Linq;
using PSB_Lib.Dependencies;
using PSB.Code.CoreSystem.Events;
using PSB.Code.CoreSystem.SaveSystem;
using PSB.Code.CoreSystem.SaveSystem.BossShop;
using PSW.Code.EventBus;
using UnityEngine;
using YIS.Code.Defines;
using YIS.Code.Events.Contents;
using YIS.Code.Items;

namespace PSB.Code.BattleCode.UIs.BossShopUI
{
    public class BossShopService : MonoBehaviour, IBossShopService, IDependencyProvider
    {
        [SerializeField] private BossItemListSO bossItemDataList;

        [SerializeField] private int maxItemPickupNum = 3;
        [SerializeField] private int maxSkillPickupNum = 3;

        [SerializeField] private List<DetailDataSO> detailUiTable;

        [Inject] private BossUnlockRepository _unlockRepository;

        private Dictionary<int, UnlockDataSO> _items;
        private Dictionary<int, UnlockDataSO> _skills;
        
        private List<UnlockDataSO> _pickUpItemTable;
        private List<UnlockDataSO> _pickUpSkillTable;
        
        private List<int> _itemKeyTable;
        private List<int> _skillKeyTable;

        [Provide]
        public IBossShopService Provide() => this;

        private void Awake()
        {
            _items = new Dictionary<int, UnlockDataSO>();
            _skills = new Dictionary<int, UnlockDataSO>();
            _pickUpItemTable = new List<UnlockDataSO>();
            _pickUpSkillTable = new List<UnlockDataSO>();

            Initialize();
        }

        public void Initialize()
        {
            _items.Clear();
            _skills.Clear();
            _pickUpItemTable.Clear();
            _pickUpSkillTable.Clear();

            if (bossItemDataList == null || bossItemDataList.shopItemDataList == null)
            {
                _itemKeyTable = new List<int>();
                _skillKeyTable = new List<int>();
                return;
            }

            foreach (var item in bossItemDataList.shopItemDataList)
            {
                if (item == null) continue;

                if (_unlockRepository != null && _unlockRepository.IsUnlocked(item.id))
                    continue;

                switch (item.shopItemData.shopItemType)
                {
                    case ShopItemType.Item:
                        if (!_items.ContainsKey(item.id))
                            _items.Add(item.id, item);
                        break;

                    case ShopItemType.Skill:
                        if (!_skills.ContainsKey(item.id))
                            _skills.Add(item.id, item);
                        break;
                }
            }

            _itemKeyTable = _items.Keys.ToList();
            _skillKeyTable = _skills.Keys.ToList();

            InitItems();
        }

        public void InitItems()
        {
            for (int i = 0; i < maxItemPickupNum; i++)
            {
                TryAddRandomItem();
            }

            for (int i = 0; i < maxSkillPickupNum; i++)
            {
                TryAddRandomSkill();
            }
        }

        private bool TryAddRandomItem()
        {
            if (_itemKeyTable == null || _itemKeyTable.Count <= 0)
                return false;

            int index = Random.Range(0, _itemKeyTable.Count);
            int itemKey = _itemKeyTable[index];

            _pickUpItemTable.Add(_items[itemKey]);

            _itemKeyTable[index] = _itemKeyTable[^1];
            _itemKeyTable.RemoveAt(_itemKeyTable.Count - 1);

            return true;
        }

        private bool TryAddRandomSkill()
        {
            if (_skillKeyTable == null || _skillKeyTable.Count <= 0)
                return false;

            int index = Random.Range(0, _skillKeyTable.Count);
            int skillKey = _skillKeyTable[index];

            _pickUpSkillTable.Add(_skills[skillKey]);

            _skillKeyTable[index] = _skillKeyTable[^1];
            _skillKeyTable.RemoveAt(_skillKeyTable.Count - 1);

            return true;
        }

        public List<UnlockDataSO> LoadItems() => _pickUpItemTable;
        public List<UnlockDataSO> LoadSkills() => _pickUpSkillTable;
        public List<DetailDataSO> LoadDetailData() => detailUiTable;

        public bool IsUnlocked(UnlockDataSO item)
        {
            if (item == null || _unlockRepository == null) return false;
            return _unlockRepository.IsUnlocked(item.id);
        }

        public bool UnlockItem(UnlockDataSO item)
        {
            if (!CurrencyContainer.Spend(ItemType.BossCoin, item.itemPrice))
            {
                return false;
            }

            if (!TryRemoveItem(item))
            {
                return false;
            }
            
            Bus<SkillUnlockEvent>.Raise(new SkillUnlockEvent(new[] { item}));
            
            return true;
        }

        private bool TryRemoveItem(UnlockDataSO item)
        {
            switch (item.shopItemData.shopItemType)
            {
                case ShopItemType.Item:
                    _pickUpItemTable.Remove(item);
                    _items.Remove(item.id);
                    TryAddRandomItem();
                    break;

                case ShopItemType.Skill:
                    _pickUpSkillTable.Remove(item);
                    _skills.Remove(item.id);
                    TryAddRandomSkill();
                    break;
            }
            Bus<BossShopRefreshEvent>.Raise(new BossShopRefreshEvent());
            return true;
        }
        
    }
}