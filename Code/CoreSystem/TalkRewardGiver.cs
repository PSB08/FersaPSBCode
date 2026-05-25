using System;
using System.Collections.Generic;
using PSB.Code.BattleCode.Enemies;
using PSB.Code.BattleCode.Items;
using PSB.Code.CoreSystem.Events;
using PSB.Code.CoreSystem.SaveSystem;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.SceneManagement;
using Work.CSH.Scripts.Relics;
using Work.PSB.Code.FieldCode;
using Work.PSB.Code.FieldCode.MapSaves;
using YIS.Code.Defines;
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
        }

        [Serializable]
        public class RewardEntry
        {
            public string rewardKey;
            public RewardType rewardType;

            public DropTableSO dropTable;
            public bool useItemDropper;
            public GameObject rewardObject;
            public Relic rewardRelic;
            public RelicDatabase relicList;
        }

        [SerializeField] private RewardEntry[] rewardEntries;
        [SerializeField] private InventoryCode inventory;

        private Dictionary<string, RewardEntry> _map;

        private void Awake()
        {
            BuildMap();
            
            Bus<TalkFinished>.OnEvent += OnTalkFinished;
        }

        private void Start()
        {
            RestoreUncollectedRewards();
        }

        private void OnDestroy()
        {
            Bus<TalkFinished>.OnEvent -= OnTalkFinished;
        }

        private void BuildMap()
        {
            _map = new Dictionary<string, RewardEntry>();
            
            if (rewardEntries == null) 
                return;
            
            foreach (var e in rewardEntries)
            {
                if (e == null || string.IsNullOrEmpty(e.rewardKey)) 
                    continue;
                _map[e.rewardKey] = e;
                
                if (e.rewardType == RewardType.ActiveObject && e.rewardObject != null)
                {
                    e.rewardObject.SetActive(false);
                }
            }
        }

        private void OnTalkFinished(TalkFinished evt)
        {
            if (string.IsNullOrEmpty(evt.RewardKey) || (evt.RewardKey.StartsWith("MINIGAME_"))) 
                return;
            
            if (_map == null) BuildMap();
            
            if (!_map.TryGetValue(evt.RewardKey, out var entry)) 
                return;

            switch(entry.rewardType)
            {
                case RewardType.ActiveObject:
                {
                    if (entry.rewardObject != null)
                    {
                        entry.rewardObject.SetActive(true);
                        Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
                    }
                    break;
                }
                case RewardType.DropTable:
                {
                    ProcessDropTableReward(entry, evt.WorldPos);
                    break;
                }
                case RewardType.GiveRelic:
                {
                    if (entry.rewardRelic != null)
                    {
                        Bus<AddRelic>.Raise(new AddRelic(entry.rewardRelic));
                        Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
                    }
                    break;
                }
                case RewardType.GiveRandomRelic:
                {
                    if (entry.relicList != null && entry.relicList.Relics.Count > 0)
                    {
                        Bus<AddRandomRelic>.Raise(new AddRandomRelic(entry.relicList));
                        Bus<RequestSaveEvent>.Raise(new RequestSaveEvent());
                    }
                    break;
                }
            }
            
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

        private bool IsCurrency(ItemType type)
        {
            return type == ItemType.Coin
                   || type == ItemType.PP || type == ItemType.BossCoin;
        }
        
        private void RestoreUncollectedRewards()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            var state = SceneSaveSystem.LoadScene(sceneName);
            
            if (state == null) return;

            var talks = SceneObjectRegistry.GetTalks();
            
            for (int i = talks.Count - 1; i >= 0; i--)
            {
                var talk = talks[i];
                if (talk == null || string.IsNullOrEmpty(talk.RewardKey)) continue;

                var talkSave = state.talks?.Find(t => t.id == talk.TalkId);
                if (talkSave != null && talkSave.isFinished)
                {
                    if (_map != null && _map.TryGetValue(talk.RewardKey, out var entry))
                    {
                        if (entry.rewardType == RewardType.ActiveObject && entry.rewardObject != null)
                        {
                            bool isBoxCollected = false;
                            var box = entry.rewardObject.GetComponentInChildren<FieldBoxCollectible>(true);
                            
                            if (box != null && !string.IsNullOrEmpty(box.BoxId))
                            {
                                var boxSave = state.boxes?.Find(b => b.id == box.BoxId);
                                if (boxSave != null)
                                {
                                    isBoxCollected = boxSave.isCollected;
                                }
                            }
                            
                            if (!isBoxCollected)
                            {
                                entry.rewardObject.SetActive(true);
                            }
                        }
                    }
                }
            }
        }
        
    }
}