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
        [SerializeField] private Grade[] skillGradeOrder =
        {
            Grade.Common,
            Grade.Uncommon,
            Grade.Rare,
            Grade.Epic,
            Grade.Legendary
        };

        [SerializeField] private List<DetailDataSO> detailUiTable;

        [Inject] private BossUnlockRepository _unlockRepository;

        private Dictionary<int, UnlockDataSO> _items;
        private Dictionary<int, UnlockDataSO> _skills;
        
        private List<UnlockDataSO> _pickUpItemTable;
        private List<UnlockDataSO> _pickUpSkillTable;
        
        private List<int> _itemKeyTable;
        private Dictionary<Grade, List<int>> _skillKeysByGrade;
        private int _skillGradeIndex;

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
                _skillKeysByGrade = new Dictionary<Grade, List<int>>();
                return;
            }

            _skillGradeIndex = 0;
            _skillKeysByGrade = new Dictionary<Grade, List<int>>();

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
                        {
                            _skills.Add(item.id, item);

                            if (item.shopItemData.skillData != null)
                                AddSkillKeyByGrade(item.shopItemData.skillData.grade, item.id);
                        }
                        break;
                }
            }

            _itemKeyTable = _items.Keys.ToList();

            InitItems();
        }

        public void InitItems()
        {
            for (int i = 0; i < maxItemPickupNum; i++)
            {
                TryAddItem();
            }

            for (int i = 0; i < maxSkillPickupNum; i++)
            {
                TryAddSkill();
            }
        }

        private bool TryAddItem()
        {
            if (_itemKeyTable == null || _itemKeyTable.Count <= 0)
                return false;

            int index = 0;
            int itemKey = _itemKeyTable[index];

            _pickUpItemTable.Add(_items[itemKey]);

            _itemKeyTable[index] = _itemKeyTable[^1];
            _itemKeyTable.RemoveAt(_itemKeyTable.Count - 1);

            return true;
        }

        private bool TryAddSkill()
        {
            if (_skillKeysByGrade == null || skillGradeOrder == null || skillGradeOrder.Length <= 0)
                return false;

            for (int i = 0; i < skillGradeOrder.Length; ++i)
            {
                Grade grade = skillGradeOrder[_skillGradeIndex];
                _skillGradeIndex = (_skillGradeIndex + 1) % skillGradeOrder.Length;

                if (!_skillKeysByGrade.TryGetValue(grade, out List<int> keys))
                    continue;

                if (keys == null || keys.Count <= 0)
                    continue;

                int index = Random.Range(0, keys.Count);
                int skillKey = keys[index];

                _pickUpSkillTable.Add(_skills[skillKey]);

                keys[index] = keys[^1];
                keys.RemoveAt(keys.Count - 1);

                return true;
            }

            return false;
        }

        private void AddSkillKeyByGrade(Grade grade, int skillKey)
        {
            if (!_skillKeysByGrade.TryGetValue(grade, out List<int> keys))
            {
                keys = new List<int>();
                _skillKeysByGrade.Add(grade, keys);
            }

            keys.Add(skillKey);
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
                    TryAddItem();
                    break;

                case ShopItemType.Skill:
                    _pickUpSkillTable.Remove(item);
                    _skills.Remove(item.id);
                    TryAddSkill();
                    break;
            }
            Bus<BossShopRefreshEvent>.Raise(new BossShopRefreshEvent());
            return true;
        }
        
    }
}
