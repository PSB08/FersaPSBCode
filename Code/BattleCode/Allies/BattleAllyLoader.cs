using System.Collections.Generic;
using PSB_Lib.Dependencies;
using PSB_Lib.ObjectPool.RunTime;
using System;
using System.Collections;
using PSW.Code.EventBus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PSB.Code.BattleCode.Allies
{
    [DefaultExecutionOrder(20)]
    public class BattleAllyLoader : MonoBehaviour
    {
        [SerializeField] private AllyDatabaseSO allyDatabase;
        [SerializeField] private BattleAlly allyPrefab;
        [SerializeField] private Transform allyParent;
        [SerializeField] private bool loadOnStart = true;

        [Header("획득 등장")]
        [SerializeField] private float rewardEntryXOffset = -15f;

        private AllyPartyService _partyService;
        [Inject] private PoolManagerMono _poolManager;
        [Inject] private BattleAllyManager _allyManager;

        private readonly HashSet<string> _spawnedIds = new();
        private readonly Dictionary<string, BattleAlly> _spawnedAllies = new();

        private void Awake()
        {
            if (Injector.Instance != null)
                Injector.Instance.InjectTo(this);
        }

        private void OnEnable()
        {
            Bus<AllyRewardResultEvent>.OnEvent += HandleAllyRewardResult;
        }

        private void OnDisable()
        {
            Bus<AllyRewardResultEvent>.OnEvent -= HandleAllyRewardResult;
        }

        private void Start()
        {
            if (loadOnStart)
                LoadEquippedAllies();
        }

        #if UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
                ResolvePartyService()?.ClearAllHealth();
        }
        #endif

        public void LoadEquippedAllies()
        {
            AllyPartyService service = ResolvePartyService();
            if (service == null)
            {
                Debug.LogWarning("[BattleAllyLoader] AllyPartyService is missing.", this);
                return;
            }

            IReadOnlyList<string> equippedIds = service.EquippedAllyIds;
            for (int i = 0; i < equippedIds.Count; i++)
                SpawnIfNeeded(equippedIds[i], i);
        }

        public bool SpawnIfNeeded(string allyId)
        {
            return SpawnIfNeeded(allyId, -1);
        }

        public bool SpawnIfNeeded(string allyId, int preferredSlotIndex)
        {
            if (string.IsNullOrWhiteSpace(allyId) || _spawnedIds.Contains(allyId))
                return false;

            if (allyDatabase == null)
            {
                Debug.LogWarning("[BattleAllyLoader] AllyDatabaseSO is missing.", this);
                return false;
            }

            if (!allyDatabase.TryGetById(allyId, out AllySO allyData))
            {
                Debug.LogWarning($"[BattleAllyLoader] Ally id not found: {allyId}", this);
                return false;
            }

            return SpawnIfNeeded(allyData, preferredSlotIndex, false);
        }

        public bool SpawnIfNeeded(AllySO allyData)
        {
            return SpawnIfNeeded(allyData, -1);
        }

        public bool SpawnIfNeeded(AllySO allyData, int preferredSlotIndex)
        {
            return SpawnIfNeeded(allyData, preferredSlotIndex, false);
        }

        private bool SpawnIfNeeded(AllySO allyData, int preferredSlotIndex, bool playEntry)
        {
            if (allyData == null)
                return false;

            string allyId = allyData.AllyId;
            if (string.IsNullOrWhiteSpace(allyId) || _spawnedIds.Contains(allyId))
                return false;

            AllyPartyService service = ResolvePartyService();
            if (service != null && service.IsDead(allyId))
            {
                Debug.Log($"[BattleAllyLoader] {allyId} is already dead. Skip battle spawn.", this);
                return false;
            }

            if (allyPrefab == null)
            {
                Debug.LogWarning("[BattleAllyLoader] Ally prefab is missing.", this);
                return false;
            }

            Transform parent = allyParent != null ? allyParent : transform;
            BattleAllyManager allyManager = ResolveAllyManager();
            Vector3 spawnPosition = ResolveSpawnPosition(allyManager, preferredSlotIndex);

            BattleAlly ally = Instantiate(allyPrefab, spawnPosition, Quaternion.identity, parent);
            ally.SetRegisterOnStart(false);

            if (preferredSlotIndex >= 0)
                ally.SetPreferredSlotIndex(preferredSlotIndex);

            ally.SetRenderersVisible(false);
            ally.buffModule?.SetPool(_poolManager);
            ally.Setup(allyData);

            bool registered = ally.RegisterToManager();
            if (!registered)
                return false;

            StartCoroutine(RevealCoroutine(ally, playEntry));

            _spawnedIds.Add(allyId);
            _spawnedAllies[allyId] = ally;
            return true;
        }

        private void HandleAllyRewardResult(AllyRewardResultEvent evt)
        {
            AllyRewardResult result = evt.Result;
            if (!result.IsRewardApplied || result.Ally == null || string.IsNullOrWhiteSpace(result.AllyId))
                return;

            string allyId = result.AllyId;
            if (TryGetSpawnedAlly(allyId, out BattleAlly spawnedAlly))
            {
                if (spawnedAlly.IsDead && result.Type == AllyRewardResultType.Revived)
                {
                    RemoveSpawnedAlly(allyId, spawnedAlly);
                    SpawnRewardAllyIfEquipped(result);
                    return;
                }

                spawnedAlly.ApplyRewardHealth(result);
                return;
            }

            SpawnRewardAllyIfEquipped(result);
        }

        private void SpawnRewardAllyIfEquipped(AllyRewardResult result)
        {
            AllyPartyService service = ResolvePartyService();
            if (service == null || !service.IsEquipped(result.AllyId))
                return;

            if (result.Type != AllyRewardResultType.Acquired &&
                result.Type != AllyRewardResultType.Revived &&
                result.Type != AllyRewardResultType.Healed)
            {
                return;
            }

            SpawnIfNeeded(result.Ally, FindEquippedSlotIndex(service, result.AllyId), true);
        }

        private bool TryGetSpawnedAlly(string allyId, out BattleAlly ally)
        {
            ally = null;
            if (string.IsNullOrWhiteSpace(allyId))
                return false;

            if (!_spawnedAllies.TryGetValue(allyId, out ally))
                return false;

            if (ally != null)
                return true;

            _spawnedAllies.Remove(allyId);
            _spawnedIds.Remove(allyId);
            return false;
        }

        private void RemoveSpawnedAlly(string allyId, BattleAlly ally)
        {
            if (!string.IsNullOrWhiteSpace(allyId))
            {
                _spawnedAllies.Remove(allyId);
                _spawnedIds.Remove(allyId);
            }

            if (ally != null)
                Destroy(ally.gameObject);
        }

        private Vector3 ResolveSpawnPosition(BattleAllyManager allyManager, int preferredSlotIndex)
        {
            if (allyManager == null)
                return transform.position;

            return preferredSlotIndex >= 0
                ? allyManager.GetAllySlotPosition(preferredSlotIndex)
                : allyManager.GetNextAllySlotPosition();
        }

        private int FindEquippedSlotIndex(AllyPartyService service, string allyId)
        {
            if (service == null || string.IsNullOrWhiteSpace(allyId))
                return -1;

            IReadOnlyList<string> equippedIds = service.EquippedAllyIds;
            for (int i = 0; i < equippedIds.Count; i++)
            {
                if (string.Equals(equippedIds[i], allyId, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private AllyPartyService ResolvePartyService()
        {
            if (_partyService != null)
                return _partyService;

            if (AllyPartyRepository.Instance != null)
            {
                _partyService = AllyPartyRepository.Instance.Service;
                return _partyService;
            }

            AllyPartyRepository repository = FindAnyObjectByType<AllyPartyRepository>(FindObjectsInactive.Include);
            _partyService = repository != null ? repository.Service : null;
            return _partyService;
        }

        private BattleAllyManager ResolveAllyManager()
        {
            if (_allyManager != null)
                return _allyManager;

            _allyManager = FindAnyObjectByType<BattleAllyManager>();
            return _allyManager;
        }
        
        private IEnumerator RevealCoroutine(BattleAlly ally, bool playEntry)
        {
            yield return null;

            if (ally == null)
                yield break;

            while (ally != null && !ally.IsHealthRestored)
                yield return null;

            if (ally == null || !ally.gameObject.activeInHierarchy || ally.IsDead)
                yield break;

            if (playEntry)
            {
                ResolveAllyManager()?.PlayEntryFromOffset(ally, rewardEntryXOffset);
                ally.SetRenderersVisible(true);
                yield break;
            }

            if (ally != null && ally.gameObject.activeInHierarchy && !ally.IsDead)
                ally.SetRenderersVisible(true);
        }
        
    }
}
