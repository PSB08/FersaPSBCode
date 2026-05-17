using System;
using System.Collections.Generic;
using System.Linq;
using PSB_Lib.Dependencies;
using PSB.Code.CoreSystem.Events;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PSB.Code.CoreSystem.SaveSystem
{
    [Serializable]
    public struct SaveData
    {
        public int SaveId;
        public string Data;
    }

    [Serializable]
    public struct DataCollection
    {
        public List<SaveData> Data;
    }
    
    [DefaultExecutionOrder(-15)]
    public class DataManager : MonoBehaviour, ISaveStore, IInventoryReader, IDependencyProvider
    {
        [SerializeField] private string saveCacheKey = "saveCache";

        private List<SaveData> _unUsedData = new();

        [Provide]
        public ISaveStore ProvideSaveStore() => this;
        [Provide]
        public IInventoryReader ProvideInvenReader() => this;

        private void Awake()
        {
            Bus<SavePrefEvent>.OnEvent += HandleSavePrefEvent;
            Bus<LoadPrefEvent>.OnEvent += HandleLoadPrefEvent;
            
            Bus<ResetAllPrefEvent>.OnEvent += HandleResetAllPrefEvent;

            HandleLoadPrefEvent(new LoadPrefEvent());
        }

        private void OnDestroy()
        {
            Bus<SavePrefEvent>.OnEvent -= HandleSavePrefEvent;
            Bus<LoadPrefEvent>.OnEvent -= HandleLoadPrefEvent;
            
            Bus<ResetAllPrefEvent>.OnEvent -= HandleResetAllPrefEvent;
        }

        #if UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                Bus<ResetAllPrefEvent>.Raise(new ResetAllPrefEvent());
            }
        }
        #endif

        private void HandleResetAllPrefEvent(ResetAllPrefEvent evt)
        {
            PlayerPrefs.DeleteKey(saveCacheKey);
            PlayerPrefs.Save();

            _unUsedData.Clear();
            IEnumerable<ISaveable> targetObjects = FindObjectsByType<MonoBehaviour>
                (FindObjectsSortMode.None).OfType<ISaveable>();

            foreach (ISaveable target in targetObjects)
            {
                target.RestoreSaveData(string.Empty);
            }
            Debug.Log("<color=red>Clear All Cathy</color>");
        }

        private void HandleSavePrefEvent(SavePrefEvent evt)
        {
            string dataJson = GetDataToJson();
            PlayerPrefs.SetString(saveCacheKey, dataJson);
            Debug.Log(dataJson);
            evt.Callback?.Invoke();
        }

        private string GetDataToJson()
        {
            IEnumerable<ISaveable> targetObjects 
                = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ISaveable>();
            
            List<SaveData> toSaveData = new List<SaveData>();

            foreach (ISaveable target in targetObjects)
            {
                toSaveData.Add(new SaveData {SaveId = target.SaveId.ID, Data = target.GetSaveData() });
            }
            toSaveData.AddRange(_unUsedData);
            DataCollection collection = new DataCollection {Data = toSaveData};
            
            return JsonUtility.ToJson(collection);
        }

        private void HandleLoadPrefEvent(LoadPrefEvent evt)
        {
            string loadedJson = PlayerPrefs.GetString(saveCacheKey, string.Empty);
            if (string.IsNullOrEmpty(loadedJson)) return;
            
            RestoreCacheData(loadedJson);
        }

        private void RestoreCacheData(string loadedJson)
        {
            IEnumerable<ISaveable> targetObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<ISaveable>();
            DataCollection collection = JsonUtility.FromJson<DataCollection>(loadedJson);

            _unUsedData.Clear();

            if (collection.Data != null)
            {
                foreach (SaveData saveData in collection.Data)
                {
                    ISaveable correctTarget =
                        targetObjects.FirstOrDefault(target => target.SaveId.ID == saveData.SaveId);
                    if (correctTarget != null)
                    {
                        correctTarget.RestoreSaveData(saveData.Data);
                    }
                    else
                    {
                        _unUsedData.Add(saveData);
                    }
                }
            }    
        }
        
        public void DeleteById(SaveId id)
        {
            string loadedJson = PlayerPrefs.GetString(saveCacheKey, string.Empty);
            if (string.IsNullOrEmpty(loadedJson)) return;

            DataCollection collection = JsonUtility.FromJson<DataCollection>(loadedJson);

            collection.Data.RemoveAll(x => x.SaveId == id.ID);
            
            PlayerPrefs.SetString(saveCacheKey, JsonUtility.ToJson(collection));
            PlayerPrefs.Save();
        }
        
        public bool TryGetRawDataById(int saveId, out string rawJson)
        {
            rawJson = null;

            string loadedJson = PlayerPrefs.GetString(saveCacheKey, string.Empty);
            if (string.IsNullOrEmpty(loadedJson)) return false;

            DataCollection collection = JsonUtility.FromJson<DataCollection>(loadedJson);
            if (collection.Data == null) return false;

            var found = collection.Data.FirstOrDefault(x => x.SaveId == saveId);
            if (string.IsNullOrEmpty(found.Data)) return false;

            rawJson = found.Data;
            return true;
        }

        public bool TryGetInventorySnapshot(int inventorySaveId, out InvenCollection snapshot)
        {
            snapshot = default;

            if (!TryGetRawDataById(inventorySaveId, out var raw))
                return false;

            snapshot = JsonUtility.FromJson<InvenCollection>(raw);
            
            snapshot.data ??= new List<InvenData>();
            return true;
        }
        
        public (int itemId, int amount)[] GetInventoryAllSlots(int inventorySaveId)
        {
            if (!TryGetInventorySnapshot(inventorySaveId, out var snap))
                return Array.Empty<(int itemId, int amount)>();

            var result = new (int itemId, int amount)[snap.slotCount];

            foreach (var e in snap.data)
            {
                if (e.slotNumber < 0 || e.slotNumber >= result.Length) continue;
                result[e.slotNumber] = (e.itemId, e.amount);
            }

            return result;
        }
        
    }
}