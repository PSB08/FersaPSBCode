using System;
using System.Collections.Generic;
using PSB.Code.BattleCode.Allies;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Events;
using PSB.Code.BattleCode.Items;
using PSB.Code.BattleCode.Skills.Sequences;
using PSB.Code.CoreSystem.Events;
using PSB.Code.CoreSystem.SaveSystem;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.CSH.Scripts.Relics;
using Work.PSB.Code.FieldCode;
using Work.PSB.Code.FieldCode.MapSaves;
using YIS.Code.Defines;
using YIS.Code.Skills.Modules;
using Random = UnityEngine.Random;

namespace Work.PSB.Code.CoreSystem
{
    public class TalkRewardGiver : MonoBehaviour
    {
        public enum RewardType
        {
            DropTable, 
            ActiveObject,
            GiveRelic,
            GiveRandomRelic,
            GiveSkill,
            DropTableAndSkill,
            GiveRandomSkill,
            HealPlayer,
            GiveAlly,
            GiveRandomAlly,
        }

        [Serializable]
        public class RewardEntry
        {
            public string rewardKey;
            public RewardType rewardType;

            public DropTableSO dropTable;
            public SkillDropTableSO skillDropTable;
            public bool useItemDropper;
            public GameObject rewardObject;
            public Relic rewardRelic;
            public RelicDatabase relicList;
            public float healValue = 0.2f;
            public HealMode healMode = HealMode.MaxPercent;
            public UIType uiType;
            public AllySO ally;
            public AllyDatabaseSO allyDatabase;
            public bool autoEquipAlly = true;
            [HideInInspector] public bool skipIfAlreadyOwned = true;
            [Range(0f, 1f)] public float duplicateAllyHealPercent = 0.3f;
        }

        [SerializeField] private RewardEntry[] rewardEntries;
        [SerializeField] private InventoryCode inventory;
        [SerializeField] private SkillContainer skillContainer;
        [SerializeField] private AllyPartyRepository allyPartyRepository;

        private Dictionary<string, List<RewardEntry>> _map;
        private AllyPartyService _allyPartyService;

        private void Awake()
        {
            BuildMap();
            ResolveReferences(null, null);
            Bus<TalkRewardRequested>.OnEvent += OnTalkRewardRequested;
        }

        private void Start()
        {
            RestoreUncollectedRewards();
        }

        private void OnDestroy()
        {
            Bus<TalkRewardRequested>.OnEvent -= OnTalkRewardRequested;
        }

        private void BuildMap()
        {
            _map = new Dictionary<string, List<RewardEntry>>();
            
            if (rewardEntries == null) 
                return;
            
            foreach (var e in rewardEntries)
            {
                if (e == null || string.IsNullOrEmpty(e.rewardKey)) 
                    continue;

                if (!_map.TryGetValue(e.rewardKey, out List<RewardEntry> entries))
                {
                    entries = new List<RewardEntry>();
                    _map.Add(e.rewardKey, entries);
                }

                entries.Add(e);
                
                if (e.rewardType == RewardType.ActiveObject && e.rewardObject != null)
                {
                    e.rewardObject.SetActive(false);
                }
            }
        }

        private void OnTalkRewardRequested(TalkRewardRequested evt)
        {
            GiveReward(evt.TalkId, evt.EnemyId, evt.RewardKey, evt.WorldPos);
        }

        public void ConfigureRuntime(RewardEntry[] entries, Transform rewardObjectParent = null,
            InventoryCode targetInventory = null, SkillContainer targetSkillContainer = null)
        {
            rewardEntries = CloneRuntimeEntries(entries, rewardObjectParent);
            ResolveReferences(targetInventory, targetSkillContainer);
            BuildMap();
        }

        public bool GiveReward(string talkId, string enemyId, string rewardKey, Vector3 worldPos)
        {
            if (string.IsNullOrEmpty(rewardKey) || rewardKey.StartsWith("MINIGAME_"))
                return false;

            if (_map == null) BuildMap();

            if (!_map.TryGetValue(rewardKey, out List<RewardEntry> entries) || entries.Count == 0)
            {
                Debug.LogWarning($"Reward key is not registered in TalkRewardGiver. RewardKey: {rewardKey}", this);
                return false;
            }

            bool rewardApplied = false;

            for (int i = 0; i < entries.Count; i++)
            {
                rewardApplied |= ProcessRewardEntry(entries[i], rewardKey, worldPos);
            }

            if (rewardApplied)
            {
                Bus<TalkRewardReceived>.Raise(new TalkRewardReceived(talkId, enemyId, rewardKey, worldPos));
            }

            return rewardApplied;
        }

        private bool ProcessRewardEntry(RewardEntry entry, string rewardKey, Vector3 worldPos)
        {
            if (entry == null)
                return false;

            switch (entry.rewardType)
            {
                case RewardType.ActiveObject:
                {
                    if (entry.rewardObject != null)
                    {
                        entry.rewardObject.SetActive(true);
                        SceneSaveSystem.SetActiveReward(SceneManager.GetActiveScene().name, rewardKey, true);
                        Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
                        return true;
                    }
                    break;
                }
                case RewardType.DropTable:
                {
                    ProcessDropTableReward(entry, worldPos);
                    return true;
                }
                case RewardType.GiveRelic:
                {
                    if (entry.rewardRelic != null)
                    {
                        Bus<AddRelic>.Raise(new AddRelic(entry.rewardRelic));
                        Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
                        return true;
                    }
                    break;
                }
                case RewardType.GiveRandomRelic:
                {
                    if (entry.relicList != null && entry.relicList.Relics.Count > 0)
                    {
                        Bus<AddRandomRelic>.Raise(new AddRandomRelic(entry.relicList));
                        Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
                        return true;
                    }
                    break;
                }
                case RewardType.GiveSkill:
                {
                    ProcessSkillDropTableReward(entry, worldPos);
                    return true;
                }

                case RewardType.GiveRandomSkill:
                {
                    return ProcessRandomSkillDropTableReward(entry, worldPos);
                }

                case RewardType.DropTableAndSkill:
                {
                    ProcessDropTableReward(entry, worldPos);
                    ProcessSkillDropTableReward(entry, worldPos);
                    return true;
                }
                case RewardType.HealPlayer:
                {
                    return ProcessHealReward(entry);
                }
                case RewardType.GiveAlly:
                {
                    return ProcessGiveAllyReward(entry, rewardKey, worldPos);
                }
                case RewardType.GiveRandomAlly:
                {
                    return ProcessGiveRandomAllyReward(entry, rewardKey, worldPos);
                }
            }

            return false;
        }

        private void ProcessDropTableReward(RewardEntry entry, Vector3 worldPos)
        {
            if (entry.dropTable == null)
                return;
            
            if (entry.useItemDropper)
            {
                ItemDropper dropper = GetComponent<ItemDropper>();
                
                if (dropper == null) 
                    return;
                
                dropper.SetDropTable(entry.dropTable);
                
                dropper.DropItemAt(new Vector3(worldPos.x, worldPos.y + 1.5f, worldPos.z), 
                    LootApplyMode.Immediate);
            }
            else
            {
                if (inventory == null)
                    ResolveReferences(null, null);

                foreach (var d in entry.dropTable.entries)
                {
                    if (d.item == null || Random.value > d.dropRate) 
                        continue;
                    
                    int amount = Random.Range(d.minAmount, d.maxAmount + 1);
                    
                    if (IsCurrency(d.item.itemType))
                    {
                        CurrencyContainer.Add(d.item.itemType, amount);
                    }
                    else if(d.item.itemType == ItemType.Item)
                    {
                        if (inventory == null)
                        {
                            Debug.LogWarning("InventoryCode is not assigned in TalkRewardGiver.", this);
                            continue;
                        }

                        for (int i = 0; i < amount; i++)
                        {
                            inventory.TryAddItem(d.item);
                        }
                    }
                    else if(d.item.itemType == ItemType.Relic)
                    {
                        Bus<AddRandomRelic>.Raise(new AddRandomRelic(d.item.relicList));
                    }
                }
            }
            Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
        }
        
        private void ProcessSkillDropTableReward(RewardEntry entry, Vector3 worldPos)
        {
            ItemDropper dropper = GetComponent<ItemDropper>();

            if (entry.skillDropTable == null || entry.skillDropTable.entries == null)
            {
                Debug.LogWarning($"Skill reward has no skill drop table. RewardKey: {entry.rewardKey}", this);
                return;
            }

            foreach (var d in entry.skillDropTable.entries)
            {
                if (d.reward == null || d.reward.skill == null)
                    continue;

                if (Random.value > d.dropRate)
                    continue;
                
                if (HasSkill(d.reward.skill))
                    continue;

                GiveSkillReward(entry, dropper, d.reward, worldPos);
            }

            Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
        }

        private bool ProcessRandomSkillDropTableReward(RewardEntry entry, Vector3 worldPos)
        {
            ItemDropper dropper = GetComponent<ItemDropper>();

            if (entry.skillDropTable == null || entry.skillDropTable.entries == null)
            {
                Debug.LogWarning($"Random skill reward has no skill drop table. RewardKey: {entry.rewardKey}", this);
                return false;
            }

            float totalWeight = 0f;

            foreach (var d in entry.skillDropTable.entries)
            {
                if (d == null || d.reward == null || d.reward.skill == null || d.dropRate <= 0f)
                    continue;

                if (HasSkill(d.reward.skill))
                    continue;

                totalWeight += d.dropRate;
            }

            if (totalWeight <= 0f)
            {
                Debug.LogWarning($"Random skill reward has no valid skill entries. RewardKey: {entry.rewardKey}", this);
                return false;
            }

            float pick = Random.value * totalWeight;

            foreach (var d in entry.skillDropTable.entries)
            {
                if (d == null || d.reward == null || d.reward.skill == null || d.dropRate <= 0f)
                    continue;

                if (HasSkill(d.reward.skill))
                    continue;

                pick -= d.dropRate;

                if (pick > 0f)
                    continue;

                GiveSkillReward(entry, dropper, d.reward, worldPos);
                Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
                return true;
            }

            return false;
        }

        private bool HasSkill(global::YIS.Code.Skills.SkillDataSO skill)
        {
            if (skillContainer == null)
            {
                Debug.LogWarning("SkillContainer is not assigned in TalkRewardGiver.", this);
                return false;
            }

            return skillContainer.HasSkill(skill);
        }

        private bool ProcessHealReward(RewardEntry entry)
        {
            if (entry.healValue <= 0f)
                return false;

            Bus<HealRequest>.Raise(new HealRequest(entry.healValue, entry.healMode));
            return true;
        }

        private bool ProcessGiveAllyReward(RewardEntry entry, string rewardKey, Vector3 worldPos)
        {
            if (entry == null || entry.ally == null)
                return false;

            AllyPartyService service = ResolveAllyPartyService();
            if (service == null)
            {
                Debug.LogWarning("AllyPartyService is missing in TalkRewardGiver.", this);
                return false;
            }

            AllyRewardResult result = service.ApplyAllyReward(
                entry.ally,
                entry.autoEquipAlly,
                entry.duplicateAllyHealPercent);

            PublishAllyRewardResult(result, rewardKey, worldPos);
            return result.IsRewardApplied;
        }

        private bool ProcessGiveRandomAllyReward(RewardEntry entry, string rewardKey, Vector3 worldPos)
        {
            if (entry == null || entry.allyDatabase == null || entry.allyDatabase.Allies == null)
                return false;

            AllyPartyService service = ResolveAllyPartyService();
            if (service == null)
            {
                Debug.LogWarning("AllyPartyService is missing in TalkRewardGiver.", this);
                return false;
            }

            List<AllySO> candidates = new List<AllySO>();
            AllySO[] allies = entry.allyDatabase.Allies;

            for (int i = 0; i < allies.Length; i++)
            {
                AllySO ally = allies[i];
                if (ally == null)
                    continue;

                candidates.Add(ally);
            }

            if (candidates.Count == 0)
                return false;

            AllySO picked = candidates[Random.Range(0, candidates.Count)];
            AllyRewardResult result = service.ApplyAllyReward(
                picked,
                entry.autoEquipAlly,
                entry.duplicateAllyHealPercent);

            PublishAllyRewardResult(result, rewardKey, worldPos);
            return result.IsRewardApplied;
        }

        private void PublishAllyRewardResult(AllyRewardResult result, string rewardKey, Vector3 worldPos)
        {
            if (!result.IsRewardApplied)
                return;

            Bus<AllyRewardResultEvent>.Raise(new AllyRewardResultEvent(result, rewardKey, worldPos));
        }

        private void GiveSkillReward(RewardEntry entry, ItemDropper dropper, SkillRewardSO reward, Vector3 worldPos)
        {
            if (reward == null || reward.skill == null)
                return;

            if (entry.useItemDropper && dropper != null && reward.skillVisualPrefab != null)
            {
                GameObject obj = dropper.CreateDropVisualObject(reward.skillVisualPrefab,
                    new Vector3(worldPos.x, worldPos.y + 1.5f, worldPos.z), true);

                if (obj != null && obj.TryGetComponent(out SkillDropVisual visual))
                {
                    visual.Init(reward);
                }
            }

            Bus<GiveSkillEvent>.Raise(new GiveSkillEvent(reward.skill));
        }

        private bool IsCurrency(ItemType type)
        {
            return type == ItemType.Coin
                   || type == ItemType.PP || type == ItemType.BossCoin;
        }

        private void ResolveReferences(InventoryCode targetInventory, SkillContainer targetSkillContainer)
        {
            if (targetInventory != null)
                inventory = targetInventory;

            if (targetSkillContainer != null)
                skillContainer = targetSkillContainer;

            if (inventory == null)
                inventory = FindAnyObjectByType<InventoryCode>(FindObjectsInactive.Include);

            if (skillContainer == null)
                skillContainer = FindAnyObjectByType<SkillContainer>(FindObjectsInactive.Include);
        }

        private AllyPartyRepository ResolveAllyPartyRepository()
        {
            if (allyPartyRepository != null)
                return allyPartyRepository;

            if (AllyPartyRepository.Instance != null)
            {
                allyPartyRepository = AllyPartyRepository.Instance;
                return allyPartyRepository;
            }

            allyPartyRepository = FindAnyObjectByType<AllyPartyRepository>(FindObjectsInactive.Include);
            return allyPartyRepository;
        }

        private AllyPartyService ResolveAllyPartyService()
        {
            if (_allyPartyService != null)
                return _allyPartyService;

            AllyPartyRepository repository = ResolveAllyPartyRepository();
            _allyPartyService = repository != null ? repository.Service : null;
            return _allyPartyService;
        }

        private RewardEntry[] CloneRuntimeEntries(RewardEntry[] source, Transform rewardObjectParent)
        {
            if (source == null || source.Length == 0)
                return source;

            RewardEntry[] clones = new RewardEntry[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                RewardEntry entry = source[i];
                if (entry == null)
                    continue;

                RewardEntry clone = new RewardEntry
                {
                    rewardKey = entry.rewardKey,
                    rewardType = entry.rewardType,
                    dropTable = entry.dropTable,
                    skillDropTable = entry.skillDropTable,
                    useItemDropper = entry.useItemDropper,
                    rewardObject = CreateRuntimeRewardObject(entry.rewardObject, rewardObjectParent),
                    rewardRelic = entry.rewardRelic,
                    relicList = entry.relicList,
                    healValue = entry.healValue,
                    healMode = entry.healMode,
                    uiType = entry.uiType,
                    ally = entry.ally,
                    allyDatabase = entry.allyDatabase,
                    autoEquipAlly = entry.autoEquipAlly,
                    skipIfAlreadyOwned = entry.skipIfAlreadyOwned,
                    duplicateAllyHealPercent = entry.duplicateAllyHealPercent
                };

                clones[i] = clone;
            }

            return clones;
        }

        private GameObject CreateRuntimeRewardObject(GameObject source, Transform parent)
        {
            if (source == null)
                return null;

            if (source.scene.IsValid())
                return source;

            Transform spawnParent = parent != null ? parent : transform;
            GameObject instance = Instantiate(source, spawnParent.position, Quaternion.identity, spawnParent);
            instance.SetActive(false);
            return instance;
        }
        
        private void RestoreUncollectedRewards()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            var state = SceneSaveSystem.LoadScene(sceneName);
            
            if (state == null) return;

            if (state.activeRewards == null)
            {
                return;
            }

            foreach (var activeReward in state.activeRewards)
            {
                if (activeReward == null || !activeReward.isActive)
                    continue;

                if (_map == null || !_map.TryGetValue(activeReward.rewardKey, out List<RewardEntry> entries))
                    continue;

                for (int i = 0; i < entries.Count; i++)
                {
                    RewardEntry entry = entries[i];

                    if (entry.rewardType != RewardType.ActiveObject || entry.rewardObject == null)
                        continue;

                    if (IsRewardObjectCollected(entry.rewardObject, state))
                        continue;

                    entry.rewardObject.SetActive(true);
                }
            }
        }

        private bool IsRewardObjectCollected(GameObject rewardObject, SceneState state)
        {
            var box = rewardObject.GetComponentInChildren<FieldBoxCollectible>(true);

            if (box == null || string.IsNullOrEmpty(box.BoxId))
                return false;

            var boxSave = state.boxes?.Find(b => b.id == box.BoxId);
            return boxSave != null && boxSave.isCollected;
        }
        
    }
}
